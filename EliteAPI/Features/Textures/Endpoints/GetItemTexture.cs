using System.Net.Mime;
using System.Text.Json.Serialization;
using EliteAPI.Features.Auth.Models;
using EliteAPI.Features.Textures.Services;
using FastEndpoints;

namespace EliteAPI.Features.Textures.Endpoints;

internal sealed class GetItemTextureRequest
{
	public string ItemId { get; set; }
	
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
		Policies(ApiUserPolicies.Admin);
		Version(0);

		Summary(s => {
			s.Summary = "Get Minecraft Item Texture";
			s.Description = "Not available to the public yet.";
		});
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