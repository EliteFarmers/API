using System.ComponentModel;
using EliteAPI.Features.Profiles.Services;
using EliteAPI.Models.Common;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Parsers.Farming;
using FastEndpoints;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Weight.GetWeightForSelected;

using Result = Results<Ok<FarmingWeightDto>, NotFound>;

internal sealed class GetWeightSelectedProfileRequest : PlayerUuidRequest {
	[QueryParam, DefaultValue(false)]
	public bool? Collections { get; set; } = false;
}

internal sealed class GetWeightForSelectedEndpoint(
	IMemberService memberService,
	AutoMapper.IMapper mapper
) : Endpoint<GetWeightSelectedProfileRequest, Result> {
	
	public override void Configure() {
		Get("/weight/{PlayerUuid}/selected");
		AllowAnonymous();
		Version(0);

		Summary(s => {
			s.Summary = "Get farming weight for a player's selected profile";
		});
		
		ResponseCache(120);
		Options(o => {
			o.CacheOutput(c => c.Expire(TimeSpan.FromMinutes(2)));
		});
	}

	public override async Task<Result> ExecuteAsync(GetWeightSelectedProfileRequest request, CancellationToken c) {
		var query = await memberService.ProfileMemberQuery(request.PlayerUuidFormatted, 16);
		if (query is null) return TypedResults.NotFound();

		var weight = await query
			.Where(x => x.IsSelected)
			.Include(x => x.Farming)
			.FirstOrDefaultAsync(cancellationToken: c);
        
		if (weight is null) return TypedResults.NotFound();
        
		var mapped = mapper.Map<FarmingWeightDto>(weight.Farming);
		if (request.Collections is not null) return TypedResults.Ok(mapped);

		mapped.Crops = weight.ExtractCropCollections()
			.ToDictionary(k => k.Key.ProperName(), v => v.Value);
        
		return TypedResults.Ok(mapped);
	}
}