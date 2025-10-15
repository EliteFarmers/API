using System.Net.Mime;
using EliteAPI.Features.Textures.Services;
using FastEndpoints;

namespace EliteAPI.Features.Textures.Endpoints;

internal sealed class GetItemTextureRequest
{
	public string ItemId { get; set; }
}

internal sealed class GetItemTextureEndpoint(
	ItemTextureResolver itemTextureResolver
) : Endpoint<GetItemTextureRequest>
{
	public override void Configure() {
		Get("/textures/{ItemId}");
		AllowAnonymous();
		Version(0);

		Summary(s => { s.Summary = "Get Minecraft Item Texture"; });
	}

	public override async Task HandleAsync(GetItemTextureRequest request, CancellationToken c) {
		// Check if itemId has a file extension and remove it
		if (request.ItemId.Contains('.')) {
			request.ItemId = request.ItemId.Split('.')[0];
		}

		var finalBytes = await itemTextureResolver.RenderItemAsync(request.ItemId);

		await Send.BytesAsync(finalBytes, contentType: MediaTypeNames.Image.Png, cancellation: c);
	}
}