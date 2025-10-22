using System.Net.Mime;
using EliteAPI.Features.Textures.Services;
using FastEndpoints;
using FastEndpoints.Swagger;

namespace EliteAPI.Features.Textures.Endpoints;

internal sealed class GetTexturePackIcon
{
	public required string PackId { get; set; }
}

internal sealed class GetTexturePackIconEndpoint(
	ItemTextureResolver itemTextureResolver
) : Endpoint<GetTexturePackIcon>
{
	public override void Configure() {
		Get("/texturepacks/{PackId}/icon");
		AllowAnonymous();
		Version(0);

		Description(d => d.AutoTagOverride("Textures"));

		Summary(s => {
			s.Summary = "Get Registered Texture Pack Icon";
			s.Description = "Retrieves the icon image for a registered texture pack by its ID.";
		});
		
		Options(o => { o.CacheOutput(c => c.Expire(TimeSpan.FromHours(1)).Tag("packicon")); });
	}

	public override async Task HandleAsync(GetTexturePackIcon request, CancellationToken c) {
		// Check if itemId has a file extension and remove it
		if (request.PackId.Contains('.')) {
			request.PackId = request.PackId.Split('.')[0];
		}

		var finalBytes = await itemTextureResolver.GetPackIcon(request.PackId);
		
		if (finalBytes is null) {
			await Send.NotFoundAsync(c);
			return;
		}

		await Send.BytesAsync(finalBytes, contentType: MediaTypeNames.Image.Png, cancellation: c);
	}
}