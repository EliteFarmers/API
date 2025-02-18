using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Models.Entities.Monetization;
using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Shop.Styles.UpdateStyle;

internal sealed class UpdateStyleRequest {
	public int StyleId { get; set; }
	[FromBody]
	public WeightStyleWithDataDto Data { get; set; } = null!;
}

internal sealed class UpdateStyleEndpoint(
	DataContext context,
	AutoMapper.IMapper mapper,
	IOutputCacheStore outputCacheStore
) : Endpoint<UpdateStyleRequest> {
	
	public override void Configure() {
		Post("/product/style/{StyleId}");
		Policies(ApiUserPolicies.Admin);
		Version(0);

		Summary(s => {
			s.Summary = "Update Shop Style";
		});
	}

	public override async Task HandleAsync(UpdateStyleRequest request, CancellationToken c) {
		var existing = await context.WeightStyles
			.FirstOrDefaultAsync(s => s.Id == request.StyleId, c);
		
		if (existing is null) {
			await SendNotFoundAsync(c);
			return;
		}
		
		var incoming = request.Data;
		existing.Name = incoming.Name ?? existing.Name;
		existing.Collection = incoming.Collection ?? existing.Collection;
		existing.StyleFormatter = incoming.StyleFormatter ?? existing.StyleFormatter;
		existing.Description = incoming.Description ?? existing.Description;
		existing.Data = incoming.Data is not null ? mapper.Map<WeightStyleData>(incoming.Data) : existing.Data;
		
		context.WeightStyles.Update(existing);
		await context.SaveChangesAsync(c);
		
		await outputCacheStore.EvictByTagAsync("styles", c);

		await SendOkAsync(cancellation: c);
	}
}

internal sealed class RequestValidator : Validator<UpdateStyleRequest> {
	public RequestValidator() {
		RuleFor(r => r.Data)
			.NotEmpty();
	}
}