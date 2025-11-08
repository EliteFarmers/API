using EliteAPI.Configuration.Settings;
using EliteAPI.Data;
using EliteAPI.Features.Resources.Auctions.DTOs;
using EliteAPI.Features.Resources.Auctions.Models;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EliteAPI.Features.Resources.Auctions.Endpoints;

public class GetAuctionVariantsRequest
{
	public required string SkyblockId { get; set; }
}

public class GetAuctionVariantsResponse
{
	public required List<AuctionItemDto> Variants { get; set; }
}

internal sealed class GetAuctionVariantsEndpoint(DataContext context, IOptions<AuctionHouseSettings> settings)
	: Endpoint<GetAuctionVariantsRequest, GetAuctionVariantsResponse>
{
	public override void Configure() {
		Get("/resources/auctions/{SkyblockId}/variants");
		AllowAnonymous();
		Version(0);

		Summary(s => {
			s.Summary = "List Auction Variants";
			s.Description = "Retrieve aggregated BIN statistics for each tracked variant of a SkyBlock item.";
		});
	}

	public override async Task HandleAsync(GetAuctionVariantsRequest req, CancellationToken ct) {
		var bundle = ParseBundleKey(req.SkyblockId);

		List<AuctionItem> variants;

		if (bundle is null) {
			variants = await context.AuctionItems
				.AsNoTracking()
				.Where(a => a.SkyblockId == req.SkyblockId)
				.OrderBy(a => a.VariantKey)
				.ToListAsync(ct);
		}
		else {
			var allowed = settings.Value.VariantOnlySkyblockIds ?? [];
			if (!allowed.Contains(bundle.Value.SkyblockId, StringComparer.OrdinalIgnoreCase)) {
				await Send.NotFoundAsync(ct);
				return;
			}

			variants = await context.AuctionItems
				.AsNoTracking()
				.Where(a => a.SkyblockId == bundle.Value.SkyblockId)
				.OrderBy(a => a.VariantKey)
				.ToListAsync(ct);

			variants = variants
				.Where(a => MatchesVariantBundle(a.VariantKey, bundle.Value))
				.ToList();
		}

		if (variants.Count == 0) {
			await Send.NotFoundAsync(ct);
			return;
		}

		var response = new GetAuctionVariantsResponse {
			Variants = variants.Select(v => v.ToDto()).ToList()
		};

		await Send.OkAsync(response, ct);
	}

	private static bool MatchesVariantBundle(string variantKey, VariantBundleRequest bundle) {
		if (string.IsNullOrWhiteSpace(variantKey)) return false;
		var variation = AuctionItemVariation.FromKey(variantKey);

		return bundle.Category switch {
			VariantBundleCategoryPet => string.Equals(variation.Pet, bundle.Identifier,
				StringComparison.OrdinalIgnoreCase),
			VariantBundleCategoryRune => variation.Extra is not null
			                             && variation.Extra.TryGetValue("rune", out var runeSpec)
			                             && IsRuneMatch(runeSpec, bundle.Identifier, bundle.Level),
			_ => false
		};
	}

	private static bool IsRuneMatch(string runeSpec, string targetRune, int? level) {
		var split = runeSpec.Split(':', StringSplitOptions.RemoveEmptyEntries);
		if (split.Length != 2) return false;
		if (!string.Equals(split[0], targetRune, StringComparison.OrdinalIgnoreCase)) return false;
		if (!level.HasValue) return true;
		return int.TryParse(split[1], out var runeLevel) && runeLevel == level.Value;
	}

	private static VariantBundleRequest? ParseBundleKey(string input) {
		if (!input.StartsWith("bundle:", StringComparison.OrdinalIgnoreCase)) return null;

		var parts = input.Split(':', StringSplitOptions.RemoveEmptyEntries);
		if (parts.Length < 3) return null;

		var category = parts[1].ToLowerInvariant();
		switch (category) {
			case VariantBundleCategoryPet:
				return new VariantBundleRequest(parts[2].ToUpperInvariant(), null, VariantBundleCategoryPet);
			case VariantBundleCategoryRune:
				int? level = null;
				if (parts.Length >= 4 && int.TryParse(parts[3], out var parsedLevel)) level = parsedLevel;
				return new VariantBundleRequest(parts[2].ToUpperInvariant(), level, VariantBundleCategoryRune);
			default:
				return null;
		}
	}

	private const string VariantBundleCategoryPet = "pet";
	private const string VariantBundleCategoryRune = "rune";

	private readonly record struct VariantBundleRequest(string Identifier, int? Level, string Category)
	{
		public string SkyblockId => Category switch {
			VariantBundleCategoryPet => "PET",
			VariantBundleCategoryRune => "RUNE",
			_ => string.Empty
		};
	}
}