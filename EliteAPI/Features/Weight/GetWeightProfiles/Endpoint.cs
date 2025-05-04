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
        
		// TODO: Remove this check after the next SkyHanni full release
		// Check for user agent (ex: "SkyHanni/0.28.Beta.15") with a version lower than "0.28.Beta.14" since it errors with the mouse property
		if (HttpContext.Request.Headers.TryGetValue("User-Agent", out var userAgent) && userAgent.ToString().Contains("SkyHanni"))
		{
			try {
				var version = userAgent.ToString().Split("/")[1].Replace("Beta.", "");
				if (System.Version.Parse(version) < System.Version.Parse("0.28.14")) {
					foreach (var profile in dto.Profiles) {
						profile.Pests.Mouse = null; // Remove mouse from the response
					}
				}
			} catch (Exception) {
				return TypedResults.Ok(dto);
			}
		}
        
		return TypedResults.Ok(dto);
	}
}