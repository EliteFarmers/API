using EliteAPI.Data;
using EliteAPI.Features.HypixelGuilds.Models;
using EliteAPI.Features.HypixelGuilds.Services;
using EliteAPI.Features.Leaderboards.Models;
using EliteAPI.Features.Leaderboards.Services;
using FastEndpoints;
using FastEndpoints.Swagger;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.HypixelGuilds.Endpoints;

internal sealed class GetHypixelGuildRankRequest
{
	public required string GuildId { get; set; }
	
	[QueryParam]
	public SortHypixelGuildsBy? SortBy { get; set; }
	
	[QueryParam]
	public string? Collection { get; set; }
	
	[QueryParam]
	public string? Skill { get; set; }
}

internal sealed class GetHypixelGuildRankResponse
{
	public required string GuildId { get; set; }
	public required string GuildName { get; set; }
	public int Rank { get; set; }
	public double Amount { get; set; }
}

internal sealed class GetHypixelGuildRankEndpoint(DataContext context, IHypixelGuildService guildService)
	: Endpoint<GetHypixelGuildRankRequest, GetHypixelGuildRankResponse>
{
	public override void Configure() {
		Get("/hguilds/{GuildId}/rank");
		AllowAnonymous();
		Version(0);

		Summary(s => {
			s.Summary = "Get Hypixel Guild Rank";
			s.Description = "Get the rank of a Hypixel guild on a specific leaderboard";
		});

		Options(o => {
			o.AutoTagOverride("Hypixel Guilds");
			o.CacheOutput(c => c.Expire(TimeSpan.FromMinutes(10)).Tag("hypixel-guild-rank"));
		});
	}

	public override async Task HandleAsync(GetHypixelGuildRankRequest request, CancellationToken c) {
		var guild = await context.HypixelGuilds
			.AsNoTracking()
			.Where(g => g.Id == request.GuildId)
			.Select(g => new { g.Id, g.Name })
			.FirstOrDefaultAsync(c);

		if (guild is null) {
			await Send.NotFoundAsync(c);
			return;
		}

		var (rank, amount) = await guildService.GetGuildRankAsync(
			request.GuildId, 
			request.SortBy, 
			request.Collection, 
			request.Skill, 
			c);
		
		await Send.OkAsync(new GetHypixelGuildRankResponse {
			GuildId = guild.Id,
			GuildName = guild.Name,
			Rank = rank,
			Amount = amount
		}, c);
	}
}

internal sealed class GetHypixelGuildRankRequestValidator : Validator<GetHypixelGuildRankRequest>
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

	public GetHypixelGuildRankRequestValidator() {
		RuleFor(x => x.GuildId)
			.NotEmpty()
			.WithMessage("GuildId is required");

		var lbService = Resolve<ILeaderboardRegistrationService>();

		RuleFor(x => x.Collection)
			.Must(CollectionExists)
			.WithMessage("Collection not valid!")
			.When(x => x.Collection is not null);

		RuleFor(x => x.Skill)
			.Must(SkillExists)
			.WithMessage("Skill not valid!")
			.When(x => x.Skill is not null);

		RuleFor(x => x)
			.Must(x => (x.Collection is not null ? 1 : 0) +
						(x.Skill is not null ? 1 : 0) +
						(x.SortBy is not null ? 1 : 0) <= 1)
			.WithMessage("Only one of Collection, Skill, or SortBy can be provided at a time!");

		return;

		bool CollectionExists(string? collection) {
			return !string.IsNullOrEmpty(collection) && lbService.LeaderboardsById.Values
				.Any(lb => lb.Info.Source == LeaderboardSourceType.Collection && lb.Info.ItemId == collection);
		}

		bool SkillExists(string? skill) {
			return !string.IsNullOrEmpty(skill) && _skillNames.Contains(skill.ToLower());
		}
	}
}

