using EliteAPI.Parsers.Inventories;
using FastEndpoints;
using MinecraftRenderer;
using MinecraftRenderer.TexturePacks;
using SkyblockRepo;

namespace EliteAPI.Features.Textures.Services;

[RegisterService<MinecraftRendererProvider>(LifeTime.Singleton)]
public class MinecraftRendererProvider
{
	private readonly TaskCompletionSource<MinecraftBlockRenderer> _initializationTcs = new();

	public MinecraftBlockRenderer.BlockRenderOptions Options { get; private set; } = null!;

	/// <summary>
	/// Gets the initialized MinecraftBlockRenderer instance asynchronously.
	/// This method will await the completion of the initialization process.
	/// </summary>
	/// <returns></returns>
	public Task<MinecraftBlockRenderer> GetRendererAsync() => _initializationTcs.Task;

	/// <summary>
	/// Initializes the MinecraftBlockRenderer asynchronously.
	/// This method should be called once during application startup.
	/// </summary>
	/// <param name="configuration"></param>
	/// <param name="repoClient"></param>
	/// <param name="logger"></param>
	public async Task InitializeAsync(
		IConfiguration configuration,
		ISkyblockRepoClient repoClient,
		ILogger logger) {
		try {
			logger.LogInformation("Starting MinecraftBlockRenderer initialization...");

			// All the long-running code from your original constructor goes here.
			// We run it on a background thread to avoid blocking.
			var renderer = await Task.Run(() => {
				var assetsPath = configuration["MinecraftRenderer:AssetsPath"]
				                 ?? Path.Combine(
					                 Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
					                 "EliteAPI");
				var texturePacksPath = Path.Combine(assetsPath, "texturepacks");
				var assetsPathRoot = Path.Combine(assetsPath, "minecraft", "assets", "minecraft");

				var texturePackRegistry = TexturePackRegistry.Create();
				texturePackRegistry.RegisterAllPacks(texturePacksPath);

				var ren = MinecraftBlockRenderer.CreateFromMinecraftAssets(
					assetsDirectory: assetsPathRoot,
					texturePackRegistry: texturePackRegistry);
				ren.PreloadRegisteredPacks();
				return ren;
			});

			Options = MinecraftBlockRenderer.BlockRenderOptions.Default with {
				Size = 128,
				SkullTextureResolver = SkullTextureResolver
			};

			NbtParser.SetRenderer(renderer);

			_initializationTcs.SetResult(renderer);
			logger.LogInformation("MinecraftBlockRenderer initialized successfully.");
		}
		catch (Exception ex) {
			logger.LogError(ex, "Failed to initialize MinecraftBlockRenderer.");
			_initializationTcs.SetException(ex);
		}
	}
	
	private string? SkullTextureResolver(MinecraftBlockRenderer.SkullResolverContext context) {
		if (context.Profile is not null || context.CustomDataId is null) return null;
		var skyblockId = context.CustomDataId;

		var match = SkyblockRepoClient.Instance.MatchItem(context);
		var skin = match?.VariantData?.Data?.Skin?.Value ?? match?.Item.Data?.Skin?.Value;

		if (skin is null && SkyblockRepoClient.Data.NeuItems.TryGetValue(skyblockId, out var item)) {
			skin = SkyblockRepoRegexUtils.ExtractSkullTexture(item.NbtTag)?.Value;
		}
					
		return skin;
	}
}