using EliteAPI.Configuration.Settings;
using MinecraftRenderer;
using MinecraftRenderer.Assets;
using Microsoft.Extensions.Options;

namespace EliteAPI.Features.Textures.Services;

public static class RendererConfiguration
{
	public static IServiceProvider AddRendererConfiguration(this IServiceCollection services) {
		// services.AddSingleton<IRendererConfiguration, RendererConfigurationService>();
		return services.BuildServiceProvider();
	}

	public static async Task DownloadMinecraftTexturesAsync(IOptions<MinecraftRendererSettings> options) {
		var settings = options.Value;
		var assetsPath = settings.ResolveAssetsPath();

		await MinecraftAssetDownloader.DownloadAndExtractAssets(
			version: settings.Version,
			outputPath: assetsPath,
			acceptEula: settings.AcceptEula
		);
	}
}
