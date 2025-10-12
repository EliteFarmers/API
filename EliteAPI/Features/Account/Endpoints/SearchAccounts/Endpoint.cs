using EliteAPI.Data;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Account.SearchAccounts;

internal sealed class SearchAccountsEndpoint(
	DataContext context
) : Endpoint<SearchRequest, List<string>> {
	public override void Configure() {
		Get("/account/search");
		AllowAnonymous();
		Version(0);

		Description(d => d.Accepts<SearchRequest>());

		Summary(s => { s.Summary = "Search for Minecraft Account"; });
	}

	public override async Task HandleAsync(SearchRequest request, CancellationToken c) {
		// Make dbParameters
		var dbQuery = new Npgsql.NpgsqlParameter("query", request.Query);
		var dbStart = new Npgsql.NpgsqlParameter("start", request.Start ?? request.Query);
		var dbEnd = new Npgsql.NpgsqlParameter("end", request.Query + "ÿ");

		// Execute autocomplete_igns stored procedure
		var result = await context.Database
			.SqlQuery<string>($"SELECT * FROM autocomplete_igns({dbQuery}, {dbStart}, {dbEnd})")
			.ToListAsync(c);

		await Send.OkAsync(result, c);
	}
}