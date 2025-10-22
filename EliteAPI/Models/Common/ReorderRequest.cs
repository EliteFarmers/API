using EliteAPI.Models.DTOs.Incoming;
using FluentValidation;

namespace EliteAPI.Models.Common;

using FastEndpoints;

public class ReorderIntRequest
{
	public List<ReorderElement<int>> Elements { get; set; } = [];
}

public class ReorderStringRequest
{
	public List<ReorderElement<string>> Elements { get; set; } = [];
}

internal sealed class ReorderIntRequestValidator : Validator<ReorderIntRequest>
{
	public ReorderIntRequestValidator() {
		RuleFor(x => x.Elements)
			.NotEmpty()
			.WithMessage("Elements is required")
			.Must(x => x.Select(e => e.Id).Distinct().Count() == x.Count)
			.WithMessage("Duplicate id values")
			.Must(x => x.Select(e => e.Order).Distinct().Count() == x.Count)
			.WithMessage("Duplicate order values");
	}
}

internal sealed class ReorderStringRequestValidator : Validator<ReorderStringRequest>
{
	public ReorderStringRequestValidator() {
		RuleFor(x => x.Elements)
			.NotEmpty()
			.WithMessage("Elements is required")
			.Must(x => x.Select(e => e.Id).Distinct().Count() == x.Count)
			.WithMessage("Duplicate id values")
			.Must(x => x.Select(e => e.Order).Distinct().Count() == x.Count)
			.WithMessage("Duplicate order values");
	}
}