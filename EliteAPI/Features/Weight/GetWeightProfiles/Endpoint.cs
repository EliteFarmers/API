using System.ComponentModel;
using EliteAPI.Data;
using EliteAPI.Models.Common;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Parsers.Farming;
using EliteAPI.Services.Interfaces;
using FastEndpoints;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Weight.GetWeightProfiles;

using Result = Results<Ok<FarmingWeightAllProfilesDto>, NotFound>;

internal sealed class GetWeightForProfilesRequest : PlayerUuidRequest {
	[QueryParam, DefaultValue(false)]
	public bool? Collections { get; set; } = false;
}

internal sealed class GetWeightForProfilesEndpoint(
	DataContext context,
	IMemberService memberService,
	AutoMapper.IMapper mapper
) : Endpoint<GetWeightForProfilesRequest, Result> {
	
	public override void Configure() {
		Get("/weight/{PlayerUuid}");
		AllowAnonymous();
		Version(0);
		
		Description(x => x.Accepts<GetWeightForProfilesRequest>());

		Summary(s => {
			s.Summary = "Get farming weight for all profiles of a player";
			s.Description = "Get farming weight for all profiles of a player";
		});
		
		ResponseCache(120);
		Options(o => {
			o.CacheOutput(c => c.Expire(TimeSpan.FromMinutes(2)));
		});
	}

	public override async Task<Result> ExecuteAsync(GetWeightForProfilesRequest request, CancellationToken c) {
		var uuid = request.PlayerUuidFormatted;
		await memberService.UpdatePlayerIfNeeded(uuid, 16);

		var members = await context.ProfileMembers
			.AsNoTracking()
			.Where(x => x.PlayerUuid.Equals(uuid) && !x.WasRemoved)
			.Include(x => x.Farming)
			.Include(x => x.Profile)
			.ToListAsync(cancellationToken: c);
        
		if (members.Count == 0)
		{
			return TypedResults.NotFound();
		}

		var dto = new FarmingWeightAllProfilesDto {
			SelectedProfileId = members.FirstOrDefault(p => p.IsSelected)?.ProfileId,
			Profiles = members.Select(m => {
				var mapped = mapper.Map<FarmingWeightWithProfileDto>(m);
				if (request.Collections is true) {
					mapped.Crops = m.ExtractCropCollections()
						.ToDictionary(k => k.Key.ProperName(), v => v.Value);
				}
				return mapped;
			}).ToList()
		};
		
		return TypedResults.Ok(dto);
	}
}