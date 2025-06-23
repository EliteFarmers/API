using System.Text.Json.Serialization;
using Riok.Mapperly.Abstractions;

namespace EliteAPI.Features.Resources.Items.Models;

public class MemberStats
{
    public double UnqiueShards { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, double>? Shards { get; set; }
}

public class MemberStatsDto
{
    public double UniqueShards { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Dictionary<string, double> Shards { get; set; } = new();
}

[Mapper]
public static partial class MemberStatsMapper
{
    public static partial MemberStatsDto ToDto(this MemberStats memberStats);
}