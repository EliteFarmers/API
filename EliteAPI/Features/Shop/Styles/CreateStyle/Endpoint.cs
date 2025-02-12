using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Models.Entities.Monetization;
using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.OutputCaching;

namespace EliteAPI.Features.Shop.Styles.CreateStyle;

internal sealed class Request {
	[FromBody]
	public WeightStyleWithDataDto Data { get; set; } = null!;
}

internal sealed class CreateStyleEndpoint(
	DataContext context,
	AutoMapper.IMapper mapper,
	IOutputCacheStore outputCacheStore
) : Endpoint<Request> {
	
	public override void Configure() {
		Post("/product/style");
		Policies(ApiUserPolicies.Admin);
		Version(0);

		Summary(s => {
			s.Summary = "Create Shop Style";
		});
	}

	public override async Task HandleAsync(Request request, CancellationToken c) {
		var newStyle = new WeightStyle {
			Name = request.Data.Name ?? "Unnamed Style",
			Collection = request.Data.Collection,
			Description = request.Data.Description
		};
		
		if (request.Data.Data is not null) {
			newStyle.Data = mapper.Map<WeightStyleData>(request.Data.Data);
		}

		if (request.Data.StyleFormatter is not null) {
			newStyle.StyleFormatter = request.Data.StyleFormatter;
		}
		
		context.WeightStyles.Add(newStyle);
		await context.SaveChangesAsync(c);
		
		await outputCacheStore.EvictByTagAsync("styles", c);

		await SendOkAsync(cancellation: c);
	}
}

internal sealed class RequestValidator : Validator<Request> {
	public RequestValidator() {
		RuleFor(r => r.Data)
			.NotEmpty();

		RuleFor(r => r.Data.Name)
			.NotEmpty()
			.MaximumLength(64);
	}
}