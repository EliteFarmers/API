using EliteAPI.Features.HypixelGuilds.Models;
using EliteAPI.Features.HypixelGuilds.Services;
using EliteAPI.Features.Leaderboards.Models;
using EliteAPI.Features.Leaderboards.Services;
using FastEndpoints;
using FastEndpoints.Swagger;
using FluentValidation;

namespace EliteAPI.Features.HypixelGuilds.Endpoints;

internal sealed class GetHypixelGuildsRequest
{
	[QueryParam]
	public SortHypixelGuildsBy? SortBy { get; set; }
	[QueryParam]
	public string? Collection { get; set; }
	[QueryParam]
	public string? Skill { get; set; }
	[QueryParam]
	public bool? Descending { get; set; } = true;
	[QueryParam]
	public int? Page { get; set; } = 1;
	[QueryParam]
	public int? PageSize { get; set; } = 50;
}

internal sealed class GetHypixelGuildsResponse
{
	public int TotalGuilds { get; set; }
	public required List<HypixelGuildDetailsDto> Guilds { get; set; }
}

internal sealed class GetHypixelGuildsEndpoint(IHypixelGuildService hypixelGuildService) : Endpoint<GetHypixelGuildsRequest, GetHypixelGuildsResponse>
{
	public override void Configure() {
		Get("/hguilds");
		AllowAnonymous();
		Version(0);

		Summary(s => { s.Summary = "Get Hypixel Guilds"; });
		
		Options(o => {
			o.AutoTagOverride("Hypixel Guilds");
			o.CacheOutput(c => c.Expire(TimeSpan.FromMinutes(10)).Tag("hypixel-guilds").SetVaryByQuery(["sortBy", "collection", "skill", "descending", "page", "pageSize"]));
		});
	}

	public override async Task HandleAsync(GetHypixelGuildsRequest request, CancellationToken c) {
		var query = new HypixelGuildListQuery {
			SortBy = request.SortBy ?? SortHypixelGuildsBy.SkyblockExperienceAverage,
			Descending = request.Descending ?? true,
			Page = request.Page ?? 1,
			PageSize = request.PageSize ?? 50,
			Collection = request.Collection,
			Skill = request.Skill
		};
		
		var result = await hypixelGuildService.GetGuildListAsync(query, c);
		
		await Send.OkAsync(new GetHypixelGuildsResponse() {
			TotalGuilds = await hypixelGuildService.GetGuildLeaderboardTotalCount(query, c),
			Guilds = result
		}, c);
	}
}

internal sealed class GetHypixelGuildsRequestValidator : Validator<GetHypixelGuildsRequest>
{
	private readonly string[] _skillNames = [
		"farming",
		"mining",
		"combat",
		"foraging",
		"fishing",
		"enchanting",
		"alchemy",
		"carpentry",
		"runecrafting",
		"taming",
		"social"
	];
	
	public GetHypixelGuildsRequestValidator() {
		RuleFor(x => x.Page)
			.GreaterThan(0)
			.WithMessage("Page must be greater than 0");

		RuleFor(x => x.PageSize)
			.InclusiveBetween(1, 100)
			.WithMessage("PageSize must be between 1 and 100");
		
		var newLbService = Resolve<ILeaderboardRegistrationService>();
		
		// If collection is provided, ensure it exists
		RuleFor(x => x.Collection)
			.Must(CollectionExists)
			.WithMessage("Collection not valid!")
			.When(x => x.Collection is not null);
		
		// If skill is provided, ensure it exists
		RuleFor(x => x.Skill)
			.Must(SkillExists)
			.WithMessage("Skill not valid!")
			.When(x => x.Skill is not null);
		
		// Check that only 1 out of collection, skill, or sortBy is provided
		RuleFor(x => x)
			.Must(x => (x.Collection is not null ? 1 : 0) +
						(x.Skill is not null ? 1 : 0) +
						(x.SortBy is not null ? 1 : 0) <= 1)
			.WithMessage("Only one of Collection, Skill, or SortBy can be provided at a time!");

		return;

		bool CollectionExists(string? collection) {
			return !string.IsNullOrEmpty(collection) && newLbService.LeaderboardsById.Values
				.Any(lb => lb.Info.Source == LeaderboardSourceType.Collection && lb.Info.ItemId == collection);
		}
		
		bool SkillExists(string? skill) {
			return !string.IsNullOrEmpty(skill) && _skillNames.Contains(skill.ToLower());
		}
	}
}