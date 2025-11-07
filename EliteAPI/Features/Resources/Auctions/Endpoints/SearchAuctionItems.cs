using EliteAPI.Data;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Resources.Auctions.Endpoints;

internal sealed class SearchAuctionItemsRequest
{
	public required string Query { get; set; }
	public int Limit { get; set; } = 10;
}

internal sealed class SearchAuctionItemsEndpoint(DataContext context)
	: Endpoint<SearchAuctionItemsRequest, SearchAuctionItemsResponse>
{
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

		var candidates = await context.SkyblockItems
			.FromSqlInterpolated($@"
                select s.*
                from ""SkyblockItems"" s
                where (
                        lower(s.""ItemId"") like lower({pattern}) escape '\'
                        or lower(coalesce(s.""Data""->>'name','')) like lower({pattern}) escape '\'
                    )
                    and exists (select 1 from ""AuctionItems"" a where a.""SkyblockId"" = s.""ItemId"")
                order by
                    case
                        when lower(s.""ItemId"") = lower({query}) then 0
                        when lower(s.""ItemId"") like lower({prefixPattern}) escape '\' then 1
                        when lower(coalesce(s.""Data""->>'name','')) like lower({prefixPattern}) escape '\' then 2
                        when lower(s.""ItemId"") like lower({pattern}) escape '\' then 3
                        else 4
                    end,
                    lower(s.""ItemId"")
                limit {limit}")
			.AsNoTracking()
			.ToListAsync(ct);

		var candidateItems = candidates
			.Select(c => new CandidateItem(c.ItemId, c.Data?.Name))
			.ToList();

		if (candidateItems.Count < limit) {
			await AugmentWithFuzzyMatches(query, candidateItems, limit, ct);
		}

		if (candidateItems.Count == 0) {
			await Send.OkAsync(new SearchAuctionItemsResponse(), ct);
			return;
		}

		var itemIds = candidateItems.Select(c => c.ItemId).Distinct().ToList();

		var variantStats = await context.AuctionItems
			.AsNoTracking()
			.Where(a => itemIds.Contains(a.SkyblockId))
			.GroupBy(a => a.SkyblockId)
			.Select(g => new {
				SkyblockId = g.Key,
				VariantCount = g.Count(),
				RecentLowest = g.Min(x => x.Lowest),
				RecentVolume = g.Sum(x => x.LowestVolume)
			})
			.ToListAsync(ct);

		var statsLookup = variantStats.ToDictionary(s => s.SkyblockId);

		var results = candidateItems.Select(item => {
				statsLookup.TryGetValue(item.ItemId, out var stats);

				return new AuctionItemSearchResult {
					SkyblockId = item.ItemId,
					Name = item.DisplayName,
					HasVariants = (stats?.VariantCount ?? 0) > 1,
					VariantCount = stats?.VariantCount ?? 0,
					RecentLowestPrice = stats?.RecentLowest,
					RecentVolume = stats?.RecentVolume ?? 0
				};
			})
			.ToList();

		await Send.OkAsync(new SearchAuctionItemsResponse { Results = results }, ct);
	}

	private async Task AugmentWithFuzzyMatches(string query, List<CandidateItem> candidates, int limit,
		CancellationToken ct) {
		var existing = new HashSet<string>(candidates.Select(c => c.ItemId), StringComparer.OrdinalIgnoreCase);

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
			if (!existing.Add(match.ItemId)) continue;

			candidates.Add(new CandidateItem(match.ItemId, match.Data?.Name));

			if (candidates.Count >= limit) break;
		}
	}

	private static double CalculateSimilarity(string query, string candidateId, string? candidateName) {
		var idScore = ComputeNormalizedSimilarity(query, candidateId);
		var nameScore = candidateName is not null
			? ComputeNormalizedSimilarity(query, candidateName)
			: 0d;
		return Math.Max(idScore, nameScore);
	}

	private static double ComputeNormalizedSimilarity(string left, string right) {
		if (string.IsNullOrEmpty(right)) return 0d;

		var leftLower = left.ToLowerInvariant();
		var rightLower = right.ToLowerInvariant();

		var distance = LevenshteinDistance(leftLower, rightLower);
		var maxLength = Math.Max(leftLower.Length, rightLower.Length);
		if (maxLength == 0) return 1d;

		return 1d - (double)distance / maxLength;
	}

	private static int LevenshteinDistance(string left, string right) {
		var n = left.Length;
		var m = right.Length;

		var d = new int[n + 1, m + 1];

		for (var i = 0; i <= n; i++) d[i, 0] = i;
		for (var j = 0; j <= m; j++) d[0, j] = j;

		for (var i = 1; i <= n; i++) {
			for (var j = 1; j <= m; j++) {
				var cost = left[i - 1] == right[j - 1] ? 0 : 1;
				d[i, j] = Math.Min(
					Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
					d[i - 1, j - 1] + cost);
			}
		}

		return d[n, m];
	}

	private sealed class CandidateItem(string itemId, string? displayName)
	{
		public string ItemId { get; } = itemId;
		public string? DisplayName { get; } = displayName;
	}
}

internal sealed class SearchAuctionItemsResponse
{
	public List<AuctionItemSearchResult> Results { get; set; } = [];
}

internal sealed class AuctionItemSearchResult
{
	public required string SkyblockId { get; set; }
	public string? Name { get; set; }
	public bool HasVariants { get; set; }
	public int VariantCount { get; set; }
	public decimal? RecentLowestPrice { get; set; }
	public int RecentVolume { get; set; }
}