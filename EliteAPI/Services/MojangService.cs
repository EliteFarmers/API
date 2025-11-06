using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using EliteAPI.Configuration.Settings;
using EliteAPI.Data;
using EliteAPI.Features.Account.Models;
using EliteAPI.Services.Interfaces;
using EliteAPI.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace EliteAPI.Services;

public partial class MojangService(
	IHttpClientFactory httpClientFactory,
	DataContext context,
	IOptions<ConfigCooldownSettings> coolDowns,
	ICacheService cacheService,
	IServiceScopeFactory serviceScopeFactory,
	ILogger<MojangService> logger)
	: IMojangService
{
	private const string ClientName = "EliteAPI";
	private readonly ConfigCooldownSettings _coolDowns = coolDowns.Value;

	public async Task<MinecraftAccount?> GetMinecraftAccountByIgn(string ign) {
		// Validate that the username is a-z, A-Z, 0-9, _ with a length of 24 or less
		if (!IgnRegex().IsMatch(ign)) return null;

		var account = await context.MinecraftAccounts
			.Include(mc => mc.Badges)
			.Where(mc => mc.Name.Equals(ign))
			.FirstOrDefaultAsync();

		if (account is null || account.LastUpdated.OlderThanSeconds(_coolDowns.MinecraftAccountCooldown)) {
			var fetched = await FetchMinecraftAccountByIgn(ign);
			return fetched ?? account;
		}

		// Get the expiry time for the cache with the last updated time in mind
		var expiry = TimeSpan.FromSeconds(_coolDowns.MinecraftAccountCooldown -
		                                  (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - account.LastUpdated));
		cacheService.SetUsernameUuidCombo(account.Name, account.Id, expiry);

		return account;
	}

	public async Task<string?> GetUsernameFromUuid(string uuid) {
		return await cacheService.GetUsernameFromUuid(uuid) ?? (await GetMinecraftAccountByUuid(uuid))?.Name;
	}

	public async Task<string?> GetUuidFromUsername(string username) {
		return await cacheService.GetUuidFromUsername(username) ?? (await GetMinecraftAccountByIgn(username))?.Id;
	}

	public async Task<string?> GetUuid(string usernameOrUuid) {
		if (usernameOrUuid.Length < 32) return await GetUuidFromUsername(usernameOrUuid);

		return usernameOrUuid;
	}

	public async Task<MinecraftAccount?> GetMinecraftAccountByUuid(string uuid) {
		var account = await context.MinecraftAccounts
			.Where(mc => mc.Id.Equals(uuid))
			.Include(mc => mc.Badges)
			.AsNoTracking()
			.FirstOrDefaultAsync();

		if (account is null || account.LastUpdated.OlderThanSeconds(_coolDowns.MinecraftAccountCooldown)) {
			var fetched = await FetchMinecraftAccountByUuid(uuid);
			return fetched ?? account;
		}

		// Get the expiry time for the cache with the last updated time in mind
		var expiry = TimeSpan.FromSeconds(_coolDowns.MinecraftAccountCooldown -
		                                  (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - account.LastUpdated));
		cacheService.SetUsernameUuidCombo(account.Name, account.Id, expiry);

		context.Entry(account).State = EntityState.Detached;

		return account;
	}

	private readonly string[] _mojangProfileUris = [
		"https://api.minecraftservices.com/minecraft/profile/lookup/name/",
		"https://mowojang.matdoes.dev/users/profiles/minecraft/",
		"https://api.mojang.com/users/profiles/minecraft/"
	];

	private async Task<MinecraftAccount?> FetchMinecraftAccountByIgn(string ign) {
		if (!IgnRegex().IsMatch(ign)) return null;

		foreach (var uri in _mojangProfileUris) {
			var result = await FetchMinecraftUuidByIgn(ign, uri);

			// Exit loop if not found, it would be redundant to try other URIs
			if (result.NotFound) return null;

			// Fetch the Minecraft account by UUID if one was returned
			if (result.Id is not null) return await FetchMinecraftAccountByUuid(result.Id);
		}

		return null;
	}

	private async Task<(string? Id, bool NotFound)> FetchMinecraftUuidByIgn(string ign, string uri) {
		var request = new HttpRequestMessage(HttpMethod.Get, uri + ign);
		var client = httpClientFactory.CreateClient(ClientName);
		client.Timeout = TimeSpan.FromSeconds(3);
		
		try {
			var response = await client.SendAsync(request);
			if (response.StatusCode == HttpStatusCode.NotFound) return (null, true);
			if (!response.IsSuccessStatusCode) return (null, false);

			var data = await response.Content.ReadFromJsonAsync<MojangProfilesResponse>();
			return (data?.Id, false);
		}
		catch (Exception) {
			logger.LogWarning("Failed to fetch Minecraft account \"{Ign}\" by IGN at {Uri}", ign, uri);
		}

		return (null, false);
	}

	public async Task<MinecraftAccount?> FetchMinecraftAccountByUuid(string uuid) {
		if (!UuidRegex().IsMatch(uuid)) return null;

		var request = new HttpRequestMessage(HttpMethod.Get,
			$"https://mowojang.matdoes.dev/session/minecraft/profile/{uuid}");
		var client = httpClientFactory.CreateClient(ClientName);
		client.Timeout = TimeSpan.FromSeconds(3);

		try {
			var response = await client.SendAsync(request);

			if (!response.IsSuccessStatusCode)
			{
				request = new HttpRequestMessage(HttpMethod.Get,
					$"https://sessionserver.mojang.com/session/minecraft/profile/{uuid}");
				response = await client.SendAsync(request);
			};

			if (!response.IsSuccessStatusCode) return null;

			var data = await response.Content.ReadFromJsonAsync<MinecraftAccountResponse>();
			if (data?.Id == null) return null;

			cacheService.SetUsernameUuidCombo(data.Name, data.Id);

			var existing = await context.MinecraftAccounts
				.Include(mc => mc.Badges)
				.Where(mc => mc.Id.Equals(data.Id))
				.FirstOrDefaultAsync();

			if (existing is not null) {
				existing.Name = data.Name;
				existing.LastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

				await StoreSkinFromProperties(data.Properties, existing);

				context.MinecraftAccounts.Update(existing);
				await context.SaveChangesAsync();
				context.Entry(existing).State = EntityState.Detached;

				return existing;
			}

			var newAccount = new MinecraftAccount {
				Id = data.Id,
				Name = data.Name,
				LastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
			};

			await StoreSkinFromProperties(data.Properties, newAccount);

			await context.MinecraftAccounts.AddAsync(newAccount);
			await context.SaveChangesAsync();

			return newAccount;
		}
		catch (Exception) {
			logger.LogWarning("Failed to fetch Minecraft account \"{Uuid}\" by UUID", uuid);
		}

		return null;
	}

	public async Task<MinecraftAccount?> GetMinecraftAccountByUuidOrIgn(string uuidOrIgn) {
		if (uuidOrIgn.Length < 32) return await GetMinecraftAccountByIgn(uuidOrIgn);

		return await GetMinecraftAccountByUuid(uuidOrIgn);
	}

	public async Task<(byte[]? face, byte[]? hat)> GetMinecraftAccountFace(string uuidOrIgn) {
		var account = await GetMinecraftAccountByUuidOrIgn(uuidOrIgn);
		return (account?.Face, account?.Hat);
	}

	public async Task<Dictionary<string, AccountMetaDto?>> GetMinecraftAccounts(List<string> uuids) {
		var dict = await context.MinecraftAccounts.AsNoTracking()
			.Include(account => account.EliteAccount)
			.Where(account => uuids.Contains(account.Id))
			.SelectMetaDto()
			.ToDictionaryAsync(account => account.Id, account => (AccountMetaDto?) account);
		
		var concurrentDict = new ConcurrentDictionary<string, AccountMetaDto?>(dict);

		await Parallel.ForEachAsync(uuids, new ParallelOptions() { MaxDegreeOfParallelism = 10 },
			async (string uuid, CancellationToken token) => {
				using var scope = serviceScopeFactory.CreateScope();
				var mojang = scope.ServiceProvider.GetRequiredService<IMojangService>();
				if (concurrentDict.ContainsKey(uuid)) return;

				var account = await mojang.GetMinecraftAccountByUuid(uuid);

				if (account is null) {
					concurrentDict.TryAdd(uuid, null);
					return;
				}

				concurrentDict.TryAdd(uuid, new AccountMetaDto() {
					Id = account.Id,
					Name = account.Name,
					FormattedName = account.Name,
				});
			});

		return concurrentDict.ToDictionary(account => account.Key, account => account.Value);
	}

	private async Task StoreSkinFromProperties(List<MinecraftAccountProperty>? properties, MinecraftAccount account) {
		var skinProperty = properties?.FirstOrDefault(p => p.Name == "textures");
		if (skinProperty is null) return;

		var b64 = skinProperty.Value;
		try {
			var textures = JsonSerializer.Deserialize<MinecraftTextures>(Convert.FromBase64String(b64));
			var skinUrl = textures?.Textures?.Skin?.Url;
			if (skinUrl is null) return;

			account.TextureId = skinUrl.Split('/').LastOrDefault();

			var request = new HttpRequestMessage(HttpMethod.Get, skinUrl);
			var client = httpClientFactory.CreateClient(ClientName);

			var response = await client.SendAsync(request);
			if (!response.IsSuccessStatusCode) return;

			await using var stream = await response.Content.ReadAsStreamAsync();
			using var originalSkin = await Image.LoadAsync(stream);

			// Use the updated helper method that no longer resizes
			using var faceIcon = CropSkinPart(originalSkin, new Rectangle(8, 8, 8, 8));
			using var hatIcon = CropSkinPart(originalSkin, new Rectangle(40, 8, 8, 8));

			byte[]? hatBytes = null;
			if (!IsImageFullyTransparent(hatIcon)) hatBytes = await ToPngBytesAsync(hatIcon);

			var faceBytes = await ToPngBytesAsync(faceIcon);

			account.Face = faceBytes;
			account.Hat = hatBytes;
		}
		catch (Exception ex) {
			logger.LogWarning(ex, "Failed to fetch skin for Minecraft account \"{Uuid}\"", account.Id);
		}
	}

	private static Image CropSkinPart(Image sourceImage, Rectangle cropArea) {
		return sourceImage.Clone(ctx => ctx.Crop(cropArea));
	}

	private static async Task<byte[]> ToPngBytesAsync(Image image) {
		using var memoryStream = new MemoryStream();
		await image.SaveAsPngAsync(memoryStream, new PngEncoder {
			CompressionLevel = PngCompressionLevel.BestCompression
		});
		return memoryStream.ToArray();
	}

	private static bool IsImageFullyTransparent(Image image) {
		using var clone = image.CloneAs<Rgba32>();

		for (var y = 0; y < clone.Height; y++) {
			for (var x = 0; x < clone.Width; x++) {
				// Check if the pixel is fully transparent
				if (clone[x, y].A != 0) return false; // Not transparent
			}
		}

		return true;
	}

	[GeneratedRegex("^[a-zA-Z0-9_]{1,24}$")]
	private static partial Regex IgnRegex();

	[GeneratedRegex("^[a-zA-Z0-9_]{32,36}$")]
	private static partial Regex UuidRegex();
}

public class MojangProfilesResponse
{
	public required string Id { get; set; }
	public required string Name { get; set; }
}

public class MinecraftTextures
{
	[JsonPropertyName("textures")] public MinecraftTexturesProperty? Textures { get; set; }
}

public class MinecraftTexturesProperty
{
	[JsonPropertyName("SKIN")] public MinecraftTextureUrl? Skin { get; set; }
	// public MinecraftTextureUrl? Cape { get; set; } // Not being used
}

public class MinecraftTextureUrl
{
	[JsonPropertyName("url")] public string? Url { get; set; }
}

public class MinecraftAccountResponse
{
	public required string Id { get; set; }
	public required string Name { get; set; }
	public List<MinecraftAccountProperty>? Properties { get; set; }
}