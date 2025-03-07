using FastEndpoints;
using FluentValidation;

namespace EliteAPI.Features.Account.SearchAccounts;

public class SearchRequest {
	/// <summary>
	/// Search query string
	/// </summary>
	[QueryParam, BindFrom("q")]
	public required string Query { get; set; }
	
	/// <summary>
	/// Start of results for pagination
	/// </summary>
	[QueryParam]
	public string? Start { get; set; }
}

internal sealed class SearchRequestValidator : Validator<SearchRequest> {
	public SearchRequestValidator() {
		RuleFor(x => x.Query)
			.NotEmpty()
			.WithMessage("Query is required")
			.Matches("^[a-zA-Z0-9_]+$")
			.WithMessage("Query must match ^[a-zA-Z0-9_]+$");
		
		RuleFor(x => x.Start)
			.Matches("^[a-zA-Z0-9_]+$")
			.When(x => x.Start is not null)
			.WithMessage("Start must match ^[a-zA-Z0-9_]+$");
	}
}