using System.Text.Json.Serialization;
using EliteAPI.Features.Textures.Services;
using FastEndpoints;

namespace EliteAPI.Features.Textures.Endpoints;

internal sealed class GetPetTextureRequest
{
	public required string PetId { get; set; }
	
	[QueryParam]
	public string? Packs { get; set; }
	
	[JsonIgnore]
	public List<string> PackList => string.IsNullOrWhiteSpace(Packs)
		? []
		: Packs.Split(',').Select(p => p.Trim()).ToList();
}

internal sealed class GetPetTextureEndpoint(
	ItemTextureResolver itemTextureResolver
) : Endpoint<GetPetTextureRequest>
{
	public override void Configure() {
		Get("/textures/pets/{PetId}");
		AllowAnonymous();
		Version(0);

		Summary(s => {
			s.Summary = "Get Skyblock Pet Texture";
		});
	}

	public override async Task HandleAsync(GetPetTextureRequest request, CancellationToken c) {
		// Check if itemId has a file extension and remove it
		if (request.PetId.Contains('.')) {
			request.PetId = request.PetId.Split('.')[0];
		}

		var path = await itemTextureResolver.RenderPetAndGetPathAsync(request.PetId, request.PackList);

		await Send.RedirectAsync(path, false, true);
	}
}