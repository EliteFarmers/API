using System.Text.Json.Serialization;

namespace EliteFarmers.HypixelAPI.DTOs;

public class HypixelGuildResponse
{
	public bool Success { get; set; }
	public RawHypixelGuild? Guild { get; set; }
}

public class RawHypixelGuild
{
	[JsonPropertyName("_id")]
	public required string Id { get; set; }
	
	[JsonPropertyName("name")]
	public required string Name { get; set; }
	
	public long Created { get; set; }

	public List<RawHypixelGuildMember> Members { get; set; } = [];
	public List<RawHypixelGuildRank> Ranks { get; set; } = [];
	
	public string? Description { get; set; }
	public List<string> PreferredGames { get; set; } = [];
	public bool PublicallyListed { get; set; }
	
	public string? Tag { get; set; }
	public string? TagColor { get; set; }
	
	public long Exp { get; set; }

	public Dictionary<string, long> GuildExpByGameType { get; set; } = new();
}

public class RawHypixelGuildMember
{
	public string Uuid { get; set; }
	public string Rank { get; set; }
	public long Joined { get; set; }
	public int QuestParticipation { get; set; }
	public long MutedTill { get; set; }
	public Dictionary<string, int> ExpHistory { get; set; } = new();
}

public class RawHypixelGuildRank
{
	public required string Name { get; set; }
	public string? Tag { get; set; }
	public bool Default { get; set; }
	public long Created { get; set; }
	public int Priority { get; set; }
}