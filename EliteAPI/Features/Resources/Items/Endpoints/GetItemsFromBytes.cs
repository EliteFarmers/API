using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Parsers.Inventories;
using FastEndpoints;

namespace EliteAPI.Features.Resources.Items.Endpoints;

internal sealed class GetItemsFromBytesRequest {
	public required string Bytes { get; set; }
}

internal sealed class GetItemsFromBytesEndpoint : Endpoint<GetItemsFromBytesRequest, GetItemsFromBytesResponse> {
	public override void Configure() {
		Post("/resources/item-parse");
		AllowAnonymous();
		Version(0);

		Summary(s => {
			s.Summary = "Parse Skyblock Item from Bytes";
			s.Description = "Get an ItemDto from raw bytes from Hypixel";
		});
	}

	public override async Task HandleAsync(GetItemsFromBytesRequest request, CancellationToken c) {
		var items = NbtParser.NbtToItems(request.Bytes);
		await Send.OkAsync(new GetItemsFromBytesResponse { Items = items ?? [] }, c);
	}
}

internal sealed class GetItemsFromBytesResponse {
	public List<ItemDto?> Items { get; set; } = [];
}