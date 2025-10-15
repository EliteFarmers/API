using EliteAPI.Features.Images.Models;
using EliteAPI.Features.Images.Services;
using FastEndpoints;

namespace EliteAPI.Features.Guilds.Services;

public interface IGuildImageService
{
	Task<Image?> UpdateGuildBannerAsync(ulong guildId, string bannerHash, Image? image = null, bool force = false);
	Task<Image?> UpdateGuildIconAsync(ulong guildId, string iconHash, Image? image = null, bool force = false);
}

[RegisterService<IGuildImageService>(LifeTime.Scoped)]
public class GuildImageService(
	IImageService imageService,
	ILogger<IGuildImageService> logger
) : IGuildImageService
{
	public async Task<Image?> UpdateGuildIconAsync(ulong guildId, string iconHash, Image? image = null,
		bool force = false) {
		try {
			if (image?.Hash == iconHash && !force) return image; // Same hash means the image is already up to date

			var iconType = iconHash.StartsWith("a_") ? "gif" : "webp";
			var remoteUrl = $"https://cdn.discordapp.com/icons/{guildId}/{iconHash}.{iconType}?size=64";
			var basePath = $"guilds/{guildId}/icons/{iconHash}";

			if (image is null)
				image = await imageService.CreateImageFromRemoteAsync(remoteUrl, basePath, "icon");
			else
				await imageService.UpdateImageFromRemoteAsync(image, remoteUrl, basePath, "icon");

			image.Hash = iconHash;

			return image;
		}
		catch (Exception e) {
			logger.LogWarning(e, "Failed to fetch guild icon from Discord for guild {GuildId}", guildId);
			return null;
		}
	}

	public async Task<Image?> UpdateGuildBannerAsync(ulong guildId, string bannerHash, Image? image = null,
		bool force = false) {
		try {
			if (image?.Hash == bannerHash && !force) return image; // Same hash means the image is already up to date

			var remoteUrl = $"https://cdn.discordapp.com/splashes/{guildId}/{bannerHash}.webp?size=1280";
			var basePath = $"guilds/{guildId}/banners/{bannerHash}";

			if (image is null)
				image = await imageService.CreateImageFromRemoteAsync(remoteUrl, basePath, "hero");
			else
				await imageService.UpdateImageFromRemoteAsync(image, remoteUrl, basePath, "hero");

			image.Hash = bannerHash;

			return image;
		}
		catch (Exception e) {
			logger.LogWarning(e, "Failed to fetch guild banner from Discord for guild {GuildId}", guildId);
			return null;
		}
	}
}