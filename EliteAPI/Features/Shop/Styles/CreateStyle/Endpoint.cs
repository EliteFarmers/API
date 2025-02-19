using System.ComponentModel.DataAnnotations;
using EliteAPI.Data;
using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Models.Entities.Monetization;
using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.OutputCaching;

namespace EliteAPI.Features.Shop.Styles.CreateStyle;

internal sealed class CreateStyleRequest {
	[MaxLength(64)]
	public string? StyleFormatter { get; set; } = "data";
	[MaxLength(64)]
	public string? Name { get; set; }
	[MaxLength(64)]
	public string? Collection { get; set; }
	[MaxLength(1024)]
	public string? Description { get; set; }
	public WeightStyleData? Data { get; set; }
}

internal sealed class CreateStyleEndpoint(
	DataContext context,
	AutoMapper.IMapper mapper,
	IOutputCacheStore outputCacheStore
) : Endpoint<CreateStyleRequest> {
	
	public override void Configure() {
		Post("/product/style");
		Policies(ApiUserPolicies.Admin);
		Version(0);

		Summary(s => {
			s.Summary = "Create Shop Style";
		});
	}

	public override async Task HandleAsync(CreateStyleRequest request, CancellationToken c) {
		var newStyle = new WeightStyle {
			Name = request.Name ?? "Unnamed Style",
			Collection = request.Collection,
			Description = request.Description
		};
		
		if (request.Data is not null) {
			newStyle.Data = mapper.Map<WeightStyleData>(request.Data);
		}

		if (request.StyleFormatter is not null) {
			newStyle.StyleFormatter = request.StyleFormatter;
		}
		
		context.WeightStyles.Add(newStyle);
		await context.SaveChangesAsync(c);
		
		await outputCacheStore.EvictByTagAsync("styles", c);

		await SendNoContentAsync(cancellation: c);
	}
}

internal sealed class RequestValidator : Validator<CreateStyleRequest> {
	public RequestValidator() {
		RuleFor(r => r.Data)
			.NotEmpty();

		RuleFor(r => r.Name)
			.NotEmpty()
			.MaximumLength(64);
	}
}