using MinecraftRenderer;
using MinecraftRenderer.Assets;

namespace EliteAPI.Features.Textures.Services;

public static class RendererConfiguration
{
	public static IServiceProvider AddRendererConfiguration(this IServiceCollection services)
	{
		// services.AddSingleton<IRendererConfiguration, RendererConfigurationService>();
		return services.BuildServiceProvider();
	}
	
	public static async Task DownloadMinecraftTexturesAsync(IConfiguration configuration)
	{
        // Get assets path from configuration or use default
        var assetsPath = configuration["MinecraftRenderer:AssetsPath"] 
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EliteAPI", "minecraft");

        await MinecraftAssetDownloader.DownloadAndExtractAssets(
            version: configuration["MinecraftRenderer:Version"] ?? "1.21.9",
            outputPath: assetsPath,
            acceptEula: configuration["MinecraftRenderer:AcceptEula"]?.ToLower() == "true"
        );
	}
}