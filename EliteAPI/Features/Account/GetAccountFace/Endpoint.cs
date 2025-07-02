using System.Net.Mime;
using EliteAPI.Models.Common;
using EliteAPI.Services.Interfaces;
using FastEndpoints;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace EliteAPI.Features.Account.GetAccountFace;

internal sealed class GetAccountFaceEndpoint(
	IMojangService mojangService
) : Endpoint<PlayerRequest> {
	
	public override void Configure() {
		Get("/account/{Player}/face", "/account/{Player}/face.png");
		AllowAnonymous();
		Version(0);
		
		// 4 hour cache
		ResponseCache(4 * 60 * 60);
		
		Summary(s => {
			s.Summary = "Get Minecraft Account Face Image";
			s.Description = "Returns an 8x8 or 72x72 face png image of the Minecraft account associated with the provided player name or UUID. 72x72 response includes the player's \"hat\" overlay. If not found, returns Steve's face.";
		});
		
		Options(o => {
			o.CacheOutput(c => c.Expire(TimeSpan.FromHours(4)));
			o.DisableRateLimiting();
		});
	}

	private static readonly byte[] SteveBase64 =
		Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAgAAAAICAYAAADED76LAAAACXBIWXMAAA7EAAAOxAGVKw4bAAAA0klEQVR42k2OvwsBcRjGn+G9O7+PgSw2VgtikB+D0SR1+QcYlfJHKDv/gEUGkzLpFlkki2I0K8f9cO6K+35Leertffo8z1sv5dKxDzyFRMK/9LeLlzek6RbkkJ+DX+nfkyQK6FVLSCbioEAYrvmEZTvQtDum6g7Ub5Qhy1He/oVMjHUrRRAz7fEMrcoISuHAL1fnGhbqEPNBx/vBA8woBWCzP/FrVmSMZWSYJnTHwHp7RDaV4YXJfIlmPY+H4YBI+uD2dGC/3jheL3xLPpGHkaCALzxDWxVDfFzPAAAAAElFTkSuQmCC");

	public override async Task HandleAsync(PlayerRequest request, CancellationToken c) {
		var (face, hat) = await mojangService.GetMinecraftAccountFace(request.Player);
		face ??= SteveBase64;
		
		if (hat is null) {
			await SendBytesAsync(face, contentType: MediaTypeNames.Image.Png, cancellation: c);
			return;
		}
		
		using var faceImage = Image.Load(face);
		
		using var largeFace = faceImage.Clone(ctx => ctx.Resize(64, 64, KnownResamplers.NearestNeighbor));
		using var finalImage = new Image<Rgba32>(72, 72);
		
		// ReSharper disable once AccessToDisposedClosure
		finalImage.Mutate(ctx => ctx.DrawImage(largeFace, new Point(4, 4), 1f));
		
		// Resize the 8x8 hat to 72x72.
		using var hatImage = Image.Load(hat);
		using var largeHat = hatImage.Clone(ctx => ctx.Resize(72, 72, KnownResamplers.NearestNeighbor));
		
		// Draw the hat on top of the face image.
		// ReSharper disable once AccessToDisposedClosure
		finalImage.Mutate(ctx => ctx.DrawImage(largeHat, new Point(0, 0), 1f));

		// Step 6: Convert the final composite image to a PNG byte array to be sent.
		using var outputStream = new MemoryStream();
		await finalImage.SaveAsPngAsync(outputStream, c); // Pass the cancellation token
		var finalBytes = outputStream.ToArray();
		
		await SendBytesAsync(finalBytes, contentType: MediaTypeNames.Image.Png, cancellation: c);
	}
}