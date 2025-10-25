using System.Text.Json.Serialization;
using EliteAPI.Features.Textures.Services;
using FastEndpoints;

namespace EliteAPI.Features.Textures.Endpoints;

internal sealed class GetItemTextureRequest
{
	public required string ItemId { get; set; }
	
	[QueryParam]
	public string? Packs { get; set; }
	
	[JsonIgnore]
	public List<string> PackList => string.IsNullOrWhiteSpace(Packs)
		? []
		: Packs.Split(',').Select(p => p.Trim()).ToList();
}

internal sealed class GetItemTextureEndpoint(
	ItemTextureResolver itemTextureResolver
) : Endpoint<GetItemTextureRequest>
{
	public override void Configure() {
		Get("/textures/{ItemId}");
		AllowAnonymous();
		Version(0);

		Summary(s => {
			s.Summary = "Get Skyblock Item Texture";
		});
	}

	public override async Task HandleAsync(GetItemTextureRequest request, CancellationToken c) {
		// Check if itemId has a file extension and remove it
		if (request.ItemId.Contains('.')) {
			request.ItemId = request.ItemId.Split('.')[0];
		}

		var path = await itemTextureResolver.RenderItemAndGetPathAsync(request.ItemId, request.PackList);

		await Send.RedirectAsync(path, false, true);
	}
}