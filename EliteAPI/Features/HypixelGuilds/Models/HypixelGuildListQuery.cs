namespace EliteAPI.Features.HypixelGuilds.Models;

public class HypixelGuildListQuery
{
	public SortHypixelGuildsBy SortBy { get; set; } = SortHypixelGuildsBy.SkyblockExperienceAverage;
	public bool Descending { get; set; } = true;
	public int Page { get; set; } = 1;
	public int PageSize { get; set; } = 50;
	public string? Collection { get; set; }
	public string? Skill { get; set; }
}

