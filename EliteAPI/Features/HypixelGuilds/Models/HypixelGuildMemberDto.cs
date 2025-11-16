using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EliteAPI.Features.Account.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EliteAPI.Features.HypixelGuilds.Models;

public class HypixelGuildMemberDto
{
	public required string PlayerUuid { get; set; }
	public required string Name { get; set; }
	public required string FormattedName { get; set; }

	public string? Rank { get; set; }

	public long JoinedAt { get; set; }
	public int QuestParticipation { get; set; }
	
	public bool Active { get; set; }

	public Dictionary<string, int> ExpHistory { get; set; } = new();
}

public class HypixelGuildMemberDetailsDto
{
	public HypixelGuildDetailsDto? Guild { get; set; }
	public string? Rank { get; set; }

	public long JoinedAt { get; set; }
	public int QuestParticipation { get; set; }
	
	public bool Active { get; set; }

	public Dictionary<string, int> ExpHistory { get; set; } = new();
}