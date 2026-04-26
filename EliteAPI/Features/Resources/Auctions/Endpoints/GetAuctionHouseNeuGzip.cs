using System.IO.Compression;
using System.Text.Json;
using EliteAPI.Features.Resources.Auctions.Services;
using FastEndpoints;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;

namespace EliteAPI.Features.Resources.Auctions.Endpoints;

internal sealed class GetAuctionHouseNeuGzipEndpoint(
	AuctionHouseNeuService auctionHouseNeuService,
	IOptions<JsonOptions> jsonOptions) : Endpoint<GetAuctionHouseNeuRequest>
{
	public override void Configure() {
		Get("/resources/auctions/neu.gz");
		AllowAnonymous();
		Version(0);

		Summary(s => {
			s.Summary = "Get Lowest BIN Prices (NEU Format, Gzip File)";
			s.Description =
				"Get the same lowest BIN data as /resources/auctions/neu, but in a gzip file. Only use this if you need to, the normal endpoint already supports gzip data transfer.";
		});

		ResponseCache(60);
		Options(o => {
			o.CacheOutput(c => c.Expire(TimeSpan.FromSeconds(60)).SetVaryByQuery("mode").Tag("auctions"));
		});
	}

	public override async Task HandleAsync(GetAuctionHouseNeuRequest req, CancellationToken c) {
		var result = await auctionHouseNeuService.BuildPricesAsync(req.Mode, c);
		var gzipBytes = await CreateGzipPayloadAsync(result, jsonOptions.Value.SerializerOptions, c);

		await Send.BytesAsync(gzipBytes, contentType: "application/gzip", cancellation: c);
	}

	internal static async Task<byte[]> CreateGzipPayloadAsync(Dictionary<string, long> payload,
		JsonSerializerOptions serializerOptions, CancellationToken cancellationToken = default) {
		using var output = new MemoryStream();

		await using (var gzipStream = new GZipStream(output, CompressionLevel.SmallestSize, leaveOpen: true)) {
			await JsonSerializer.SerializeAsync(gzipStream, payload, serializerOptions, cancellationToken);
		}

		return output.ToArray();
	}
}