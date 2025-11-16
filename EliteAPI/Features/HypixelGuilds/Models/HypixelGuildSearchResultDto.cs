namespace EliteAPI.Features.HypixelGuilds.Models;

public class HypixelGuildSearchResultDto
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public int MemberCount { get; set; }
    public string? Tag { get; set; }
    public string? TagColor { get; set; }
}

