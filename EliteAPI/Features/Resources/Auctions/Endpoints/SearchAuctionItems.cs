using EliteAPI.Configuration.Settings;
using EliteAPI.Data;
using EliteAPI.Features.Resources.Auctions.Models;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Text;
using EliteAPI.Utilities;

namespace EliteAPI.Features.Resources.Auctions.Endpoints;

internal sealed class SearchAuctionItemsRequest
{
	public required string Query { get; set; }
	public int Limit { get; set; } = 10;
}

internal sealed class SearchAuctionItemsEndpoint(DataContext context, IOptions<AuctionHouseSettings> settings)
	: Endpoint<SearchAuctionItemsRequest, SearchAuctionItemsResponse>
{
	private readonly HashSet<string> _variantOnlySkyblockIds = new(settings.Value.VariantOnlySkyblockIds ?? new List<string>(), StringComparer.OrdinalIgnoreCase);

	public override void Configure() {
		Get("/resources/auctions/search");
		AllowAnonymous();
		Version(0);

		Summary(s => {
			s.Summary = "Search Auction Items";
			s.Description = "Fuzzy search across known auctionable items by SkyBlock ID or display name.";
		});
	}

	public override async Task HandleAsync(SearchAuctionItemsRequest req, CancellationToken ct) {
		var query = req.Query?.Trim();
		if (string.IsNullOrWhiteSpace(query)) {
			await Send.OkAsync(new SearchAuctionItemsResponse(), ct);
			return;
		}

		var limit = Math.Clamp(req.Limit, 1, 50);

		static string EscapeLike(string value) {
			return value
				.Replace("\\", "\\\\", StringComparison.Ordinal)
				.Replace("%", "\\%", StringComparison.Ordinal)
				.Replace("_", "\\_", StringComparison.Ordinal);
		}

		var escaped = EscapeLike(query);
		var pattern = $"%{escaped}%";
		var prefixPattern = $"{escaped}%";

		var baseCandidates = await context.SkyblockItems
			.FromSqlInterpolated($@"
                select s.*
                from ""SkyblockItems"" s
                where (
						lower(s.""ItemId"") like lower({pattern})
						or lower(coalesce(s.""Data""->>'name','')) like lower({pattern})
                    )
                    and exists (select 1 from ""AuctionItems"" a where a.""SkyblockId"" = s.""ItemId"")
                order by
                    case
                        when lower(s.""ItemId"") = lower({query}) then 0
						when lower(s.""ItemId"") like lower({prefixPattern}) then 1
						when lower(coalesce(s.""Data""->>'name','')) like lower({prefixPattern}) then 2
						when lower(s.""ItemId"") like lower({pattern}) then 3
                        else 4
                    end,
                    lower(s.""ItemId"")
                limit {limit}")
			.AsNoTracking()
			.ToListAsync(ct);

		var candidates = baseCandidates
			.Select(c => new CandidateItem(c.ItemId, c.Data?.Name))
			.Where(c => !_variantOnlySkyblockIds.Contains(c.ItemId))
			.ToList();

		if (candidates.Count < limit) {
			await AugmentWithFuzzyMatches(query, candidates, limit, ct);
		}

		var variantBundles = await BuildVariantBundleCandidates(query, limit, ct);

		var existingKeys = new HashSet<string>(candidates.Select(c => CreateCandidateKey(c.ItemId, c.VariantKey)), StringComparer.Ordinal);
		foreach (var bundleCandidate in variantBundles) {
			if (!existingKeys.Add(CreateCandidateKey(bundleCandidate.ItemId, bundleCandidate.VariantKey))) continue;
			candidates.Add(bundleCandidate);
		}

		if (candidates.Count == 0) {
			await Send.OkAsync(new SearchAuctionItemsResponse(), ct);
			return;
		}

		foreach (var candidate in candidates) {
			if (candidate.Score <= 0) {
				candidate.Score = CalculateSimilarity(query, candidate.ItemId, candidate.DisplayName, candidate.VariantKey);
			}
		}

		candidates = candidates
			.OrderByDescending(c => c.Score)
			.ThenBy(c => c.DisplayName ?? c.ItemId, StringComparer.OrdinalIgnoreCase)
			.Take(limit)
			.ToList();

		var baseItemIds = candidates
			.Where(c => !c.IsVariant)
			.Select(c => c.ItemId)
			.Distinct()
			.ToList();

		var baseStats = baseItemIds.Count == 0
			? []
			: await context.AuctionItems
				.AsNoTracking()
				.Where(a => baseItemIds.Contains(a.SkyblockId))
				.GroupBy(a => a.SkyblockId)
				.Select(g => new {
					SkyblockId = g.Key,
					VariantCount = g.Count(),
					RecentLowest = g.Min(x => x.Lowest),
					RecentVolume = g.Sum(x => x.LowestVolume)
				})
				.ToListAsync(ct);

		var statsLookup = baseStats.ToDictionary(s => s.SkyblockId, StringComparer.OrdinalIgnoreCase);

		var results = candidates.Select(candidate => {
			if (candidate.IsVariant) {
				return new AuctionItemSearchResult {
					SkyblockId = candidate.ItemId,
					VariantKey = candidate.VariantKey,
					Name = candidate.DisplayName,
					HasVariants = false,
					VariantCount = candidate.VariantCount,
					RecentLowestPrice = candidate.RecentLowestPrice,
					RecentVolume = candidate.RecentVolume
				};
			}

			statsLookup.TryGetValue(candidate.ItemId, out var stats);
			return new AuctionItemSearchResult {
				SkyblockId = candidate.ItemId,
				VariantKey = candidate.VariantKey,
				Name = candidate.DisplayName,
				HasVariants = (stats?.VariantCount ?? 0) > 1,
				VariantCount = stats?.VariantCount ?? 0,
				RecentLowestPrice = stats?.RecentLowest,
				RecentVolume = stats?.RecentVolume ?? 0
			};
		}).ToList();

		await Send.OkAsync(new SearchAuctionItemsResponse { Results = results }, ct);
	}

	private async Task AugmentWithFuzzyMatches(string query, List<CandidateItem> candidates, int limit,
		CancellationToken ct) {
		var existing = new HashSet<string>(candidates.Select(c => CreateCandidateKey(c.ItemId, c.VariantKey)), StringComparer.Ordinal);

		var fallbackPool = await context.SkyblockItems
			.AsNoTracking()
			.Where(s => context.AuctionItems.Any(a => a.SkyblockId == s.ItemId))
			.Select(s => new { s.ItemId, s.Data })
			.ToListAsync(ct);

		var scored = fallbackPool
			.Select(item => new {
				item.ItemId,
				item.Data,
				Score = CalculateSimilarity(query, item.ItemId, item.Data?.Name)
			})
			.Where(x => x.Score >= 0.3)
			.OrderByDescending(x => x.Score)
			.ThenBy(x => x.ItemId, StringComparer.OrdinalIgnoreCase)
			.ToList();

		foreach (var match in scored) {
			if (_variantOnlySkyblockIds.Contains(match.ItemId)) continue;
			if (!existing.Add(CreateCandidateKey(match.ItemId, null))) continue;

			candidates.Add(new CandidateItem(match.ItemId, match.Data?.Name) { Score = match.Score });

			if (candidates.Count >= limit) break;
		}
	}

	private async Task<List<CandidateItem>> BuildVariantBundleCandidates(string query, int limit,
		CancellationToken ct) {
		var variants = await context.AuctionItems
			.AsNoTracking()
			.Where(a => _variantOnlySkyblockIds.Contains(a.SkyblockId))
			.Select(a => new {
				a.SkyblockId,
				a.VariantKey,
				a.Lowest,
				a.LowestVolume
			})
			.ToListAsync(ct);

		if (variants.Count == 0) return [];

		var bundles = new Dictionary<string, VariantBundle>(StringComparer.OrdinalIgnoreCase);

		foreach (var variant in variants) {
			if (string.IsNullOrWhiteSpace(variant.VariantKey)) continue;
			var variation = AuctionItemVariation.FromKey(variant.VariantKey);
			var descriptor = CreateBundleDescriptor(variant.SkyblockId, variation);
			if (descriptor is null) continue;

			var cacheKey = CreateCandidateKey(variant.SkyblockId, descriptor.Value.BundleKey);
			if (!bundles.TryGetValue(cacheKey, out var bundle)) {
				bundle = new VariantBundle(descriptor.Value.SkyblockId, descriptor.Value.BundleKey,
					descriptor.Value.DisplayName, descriptor.Value.SearchTokens);
				bundles[cacheKey] = bundle;
			}

			bundle.AddVariant(variant.VariantKey, variant.Lowest, variant.LowestVolume);
		}

		var queryLength = query.Length;
		var bundleCandidates = new List<CandidateItem>();

		foreach (var bundle in bundles.Values) {
			var scoreTargets = new List<string?> { bundle.DisplayName, bundle.BundleKey };
			scoreTargets.AddRange(bundle.SearchTokens);
			var score = CalculateSimilarity(query, scoreTargets.ToArray());
			var contains = bundle.DisplayName?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false;
			if (!contains && queryLength > 3 && score < 0.2) continue;

			bundleCandidates.Add(new CandidateItem(bundle.SkyblockId, bundle.DisplayName, bundle.BundleKey, true) {
				Score = Math.Max(score, contains ? 0.95 : score),
				RecentLowestPrice = bundle.RecentLowest,
				RecentVolume = bundle.RecentVolume,
				VariantCount = bundle.VariantCount
			});
		}

		return bundleCandidates
			.OrderByDescending(c => c.Score)
			.ThenBy(c => c.DisplayName ?? c.ItemId, StringComparer.OrdinalIgnoreCase)
			.Take(limit)
			.ToList();
	}

	private static VariantBundleDescriptor? CreateBundleDescriptor(string skyblockId, AuctionItemVariation variation) {
		switch (skyblockId.ToUpperInvariant()) {
			case "PET":
				if (string.IsNullOrWhiteSpace(variation.Pet)) return null;
				var petId = variation.Pet.ToUpperInvariant();
				var petName = FormatIdentifier(petId);
				var displayName = petName + " Pet";
				var bundleKey = $"bundle:pet:{petId}";
				var searchTokens = new List<string> {
					petId,
					petName,
					petName + " Pet"
				};
				if (!string.IsNullOrWhiteSpace(variation.Rarity)) searchTokens.Add(variation.Rarity);
				if (!string.IsNullOrWhiteSpace(variation.PetLevel?.Key)) searchTokens.Add(variation.PetLevel!.Key);
				return new VariantBundleDescriptor(skyblockId, bundleKey, displayName, searchTokens);

			case "RUNE":
			case "UNIQUE_RUNE":
				if (variation.Extra is null || !variation.Extra.TryGetValue("rune", out var runeSpec)) return null;
				var parts = runeSpec.Split(':', StringSplitOptions.RemoveEmptyEntries);
				if (parts.Length != 2) return null;
				var runeId = parts[0].ToUpperInvariant();
				var levelIdentifier = parts[1];
				var runeName = FormatIdentifier(runeId);
				var bundleLevel = int.TryParse(levelIdentifier, out var level) ? level : (int?)null;
				var roman = bundleLevel is not null ? ToRomanNumeral(bundleLevel.Value) : levelIdentifier;
				var display = bundleLevel is not null
					? $"{runeName} Rune {roman}"
					: $"{runeName} Rune";
				var runeBundleKey = bundleLevel is not null
					? $"bundle:rune:{runeId}:{bundleLevel.Value}"
					: $"bundle:rune:{runeId}";
				var runeTokens = new List<string> {
					runeId,
					runeName,
					runeName + " Rune",
					roman
				};
				if (!string.IsNullOrWhiteSpace(levelIdentifier)) runeTokens.Add(levelIdentifier);
				return new VariantBundleDescriptor(skyblockId, runeBundleKey, display, runeTokens);
		}

		return null;
	}

	private static double CalculateSimilarity(string query, params string?[] candidates) {
		double best = 0d;
		foreach (var candidate in candidates) {
			if (string.IsNullOrWhiteSpace(candidate)) continue;
			best = Math.Max(best, ComputeNormalizedSimilarity(query, candidate));
		}
		return best;
	}

	private static double ComputeNormalizedSimilarity(string left, string right) {
		if (string.IsNullOrEmpty(right)) return 0d;

		var leftLower = left.ToLowerInvariant();
		var rightLower = right.ToLowerInvariant();

		var distance = FormatUtils.LevenshteinDistance(leftLower, rightLower);
		var maxLength = Math.Max(leftLower.Length, rightLower.Length);
		if (maxLength == 0) return 1d;

		return 1d - (double)distance / maxLength;
	}
	
	private static string CreateCandidateKey(string itemId, string? variantKey) {
		var variantPart = variantKey is null ? string.Empty : variantKey.ToUpperInvariant();
		return string.Concat(itemId.ToUpperInvariant(), "::", variantPart);
	}

	private static string FormatIdentifier(string value) {
		if (string.IsNullOrWhiteSpace(value)) return value;
		var tokens = value.Split(new[] { '_', '-' }, StringSplitOptions.RemoveEmptyEntries);
		if (tokens.Length == 0) return value;
		var builder = new StringBuilder();
		for (var i = 0; i < tokens.Length; i++) {
			var token = tokens[i];
			if (token.Length == 0) continue;
			if (i > 0) builder.Append(' ');
			builder.Append(char.ToUpperInvariant(token[0]));
			if (token.Length > 1) builder.Append(token[1..].ToLowerInvariant());
		}
		return builder.Length > 0 ? builder.ToString() : value;
	}

	private static string ToRomanNumeral(int value) {
		if (value <= 0) return value.ToString(CultureInfo.InvariantCulture);
		var numerals = new (int Value, string Numeral)[] {
			(1000, "M"), (900, "CM"), (500, "D"), (400, "CD"),
			(100, "C"), (90, "XC"), (50, "L"), (40, "XL"),
			(10, "X"), (9, "IX"), (5, "V"), (4, "IV"), (1, "I")
		};

		var remainder = value;
		var builder = new StringBuilder();
		foreach (var (threshold, symbol) in numerals) {
			while (remainder >= threshold) {
				builder.Append(symbol);
				remainder -= threshold;
			}
		}

		return builder.Length > 0 ? builder.ToString() : value.ToString(CultureInfo.InvariantCulture);
	}

	private readonly struct VariantBundleDescriptor(string skyblockId, string bundleKey, string displayName,
		IReadOnlyCollection<string> searchTokens)
	{
		public string SkyblockId { get; } = skyblockId;
		public string BundleKey { get; } = bundleKey;
		public string DisplayName { get; } = displayName;
		public IReadOnlyCollection<string> SearchTokens { get; } = searchTokens;
	}

	private sealed class VariantBundle
	{
		private readonly HashSet<string> _variantKeys = new(StringComparer.Ordinal);
		private readonly List<string> _searchTokens;
		private decimal? _recentLowest;
		private int _recentVolume;

		public VariantBundle(string skyblockId, string bundleKey, string displayName, IReadOnlyCollection<string> searchTokens) {
			SkyblockId = skyblockId;
			BundleKey = bundleKey;
			DisplayName = displayName;
			_searchTokens = new List<string>(searchTokens);
		}

		public string SkyblockId { get; }
		public string BundleKey { get; }
		public string DisplayName { get; }
		public IReadOnlyCollection<string> SearchTokens => _searchTokens;
		public decimal? RecentLowest => _recentLowest;
		public int RecentVolume => _recentVolume;
		public int VariantCount => _variantKeys.Count;

		public void AddVariant(string variantKey, decimal? lowest, int lowestVolume) {
			_variantKeys.Add(variantKey);
			if (lowest is not null) {
				_recentLowest = _recentLowest is null ? lowest : Math.Min(_recentLowest.Value, lowest.Value);
			}
			_recentVolume += Math.Max(0, lowestVolume);
		}
	}

	private sealed class CandidateItem
	{
		public CandidateItem(string itemId, string? displayName, string? variantKey = null, bool isVariant = false) {
			ItemId = itemId;
			DisplayName = displayName;
			VariantKey = variantKey;
			IsVariant = isVariant;
		}

		public string ItemId { get; }
		public string? DisplayName { get; }
		public string? VariantKey { get; }
		public bool IsVariant { get; }
		public double Score { get; set; }
		public decimal? RecentLowestPrice { get; set; }
		public int RecentVolume { get; set; }
		public int VariantCount { get; set; }
	}
}

internal sealed class SearchAuctionItemsResponse
{
	public List<AuctionItemSearchResult> Results { get; set; } = [];
}

internal sealed class AuctionItemSearchResult
{
	public required string SkyblockId { get; set; }
	public string? VariantKey { get; set; }
	public string? Name { get; set; }
	public bool HasVariants { get; set; }
	public int VariantCount { get; set; }
	public decimal? RecentLowestPrice { get; set; }
	public int RecentVolume { get; set; }
}