using System.Text.Json;
using System.Text.Json.Serialization;

namespace HypixelAPI.DTOs;

public class PlayerStatsResponse : IJsonOnDeserialized
{
    [JsonPropertyName("unique_shards"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public double UniqueShards { get; set; }
    
    [JsonIgnore]
    public Dictionary<string, double> AppliedShards { get; set; } = new();
    
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
    
    void IJsonOnDeserialized.OnDeserialized()
    {
        if (ExtensionData is null) return;
        const string shardPrefix = "shard_";
        // Now, process the extra properties we caught

        var shards = ExtensionData
            .Where(property => property.Key.StartsWith(shardPrefix))
            .Where(property => property.Value.ValueKind == JsonValueKind.Number);
        
        foreach (var property in shards)
        {
            AppliedShards.Add(property.Key, property.Value.GetDouble());
            ExtensionData.Remove(property.Key);
        }
    }
}