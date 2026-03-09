using EliteAPI.Configuration.Settings;
using Microsoft.Extensions.Options;
using SkyblockRepo;

namespace EliteAPI.Features.Textures.Services;

public class MinecraftRendererInitializer(
	IServiceProvider serviceProvider,
	ILogger<MinecraftRendererInitializer> logger) : IHostedService
{
	public Task StartAsync(CancellationToken cancellationToken) {
		logger.LogInformation("Minecraft Renderer Initializer is starting.");

		_ = Task.Run(async () => {
			// Create a new scope to resolve scoped services like ISkyblockRepoClient
			await using var scope = serviceProvider.CreateAsyncScope();
			var scopedProvider = scope.ServiceProvider;

			var rendererOptions = scopedProvider.GetRequiredService<IOptions<MinecraftRendererSettings>>();
			await RendererConfiguration.DownloadMinecraftTexturesAsync(rendererOptions);

			var rendererProvider = scopedProvider.GetRequiredService<MinecraftRendererProvider>();
			var repoClient = scopedProvider.GetRequiredService<ISkyblockRepoClient>();
			var initLogger = scopedProvider.GetRequiredService<ILogger<MinecraftRendererProvider>>();

			await rendererProvider.InitializeAsync(rendererOptions, repoClient, initLogger);
		}, cancellationToken);

		return Task.CompletedTask;
	}

	public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
