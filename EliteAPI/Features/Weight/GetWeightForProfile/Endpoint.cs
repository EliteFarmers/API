using System.ComponentModel;
using EliteAPI.Models.Common;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Parsers.Farming;
using EliteAPI.Services.Interfaces;
using FastEndpoints;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Weight.GetWeightForProfile;

using Result = Results<Ok<FarmingWeightDto>, NotFound>;

internal sealed class GetWeightProfilesRequest : PlayerProfileUuidRequest {
	[QueryParam, DefaultValue(false)]
	public bool? Collections { get; set; } = false;
}

internal sealed class GetWeightForProfileEndpoint(
	IMemberService memberService,
	AutoMapper.IMapper mapper
) : Endpoint<GetWeightProfilesRequest, Result> {
	
	public override void Configure() {
		Get("/weight/{PlayerUuid}/{ProfileUuid}");
		AllowAnonymous();
		Version(0);

		Summary(s => {
			s.Summary = "Get farming weight for a profile member";
		});
	}

	public override async Task<Result> ExecuteAsync(GetWeightProfilesRequest request, CancellationToken c) {
		var query = await memberService.ProfileMemberQuery(request.PlayerUuidFormatted, 3);
		if (query is null) return TypedResults.NotFound();

		var weight = await query
			.Where(x => x.IsSelected && x.ProfileId.Equals(request.ProfileUuidFormatted))
			.Include(x => x.Farming)
			.FirstOrDefaultAsync(cancellationToken: c);

		if (weight is null) return TypedResults.NotFound();
        
		var mapped = mapper.Map<FarmingWeightDto>(weight.Farming);
		if (request.Collections is not true) return TypedResults.Ok(mapped);

		mapped.Crops = weight.ExtractCropCollections()
			.ToDictionary(k => k.Key.ProperName(), v => v.Value);
        
		return TypedResults.Ok(mapped);
	}
}