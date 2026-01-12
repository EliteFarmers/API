using System.Text.Json.Serialization;
using EliteAPI.Features.Textures.Services;
using FastEndpoints;

namespace EliteAPI.Features.Textures.Endpoints;

internal sealed class GetBlockTextureRequest
{
	public required string BlockId { get; set; }

	[QueryParam] public string? Packs { get; set; }

	[QueryParam] public string? Face { get; set; }

	[JsonIgnore]
	public List<string> PackList => string.IsNullOrWhiteSpace(Packs)
		? []
		: Packs.Split(',').Select(p => p.Trim()).ToList();
}

internal sealed class GetBlockTextureEndpoint(
	ItemTextureResolver itemTextureResolver
) : Endpoint<GetBlockTextureRequest>
{
	public override void Configure() {
		Get("/textures/blocks/{BlockId}");
		AllowAnonymous();
		Version(0);

		Summary(s => { s.Summary = "Get Minecraft Block Texture"; });

		Options(o => {
			o.DisableRateLimiting();
			o.CacheOutput(c => c.Expire(TimeSpan.FromMinutes(30)));
		});
	}

	public override async Task HandleAsync(GetBlockTextureRequest request, CancellationToken c) {
		// Check if itemId has a file extension and remove it
		if (request.BlockId.Contains('.')) {
			request.BlockId = request.BlockId.Split('.')[0];
		}

		var bytes = await itemTextureResolver.RenderBlockFace(request.BlockId, request.PackList, 16);

		await Send.BytesAsync(bytes, contentType: "image/png", cancellation: c);
	}
}