using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EliteFarmers.HypixelAPI.DTOs;

[JsonConverter(typeof(TrophyFishStatsConverter))]
public sealed class TrophyFishStats
{
    public List<int> Rewards { get; set; } = [];
    public int? TotalCaught { get; set; }
    public string? LastCaught { get; set; }
    public Dictionary<string, int> FishCounts { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class TrophyFishStatsConverter : JsonConverter<TrophyFishStats>
{
    public override TrophyFishStats? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected StartObject for trophy_fish stats");
        }

        var result = new TrophyFishStats();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Expected PropertyName within trophy_fish stats");
            }

            var propertyName = reader.GetString() ?? string.Empty;

            if (!reader.Read())
            {
                throw new JsonException("Unexpected end when reading trophy_fish property value");
            }

            switch (propertyName)
            {
                case "rewards":
                    if (reader.TokenType == JsonTokenType.StartArray)
                    {
                        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                        {
                            if (reader.TokenType == JsonTokenType.Number)
                            {
                                if (reader.TryGetInt32(out var reward))
                                {
                                    result.Rewards.Add(reward);
                                }
                                else if (reader.TryGetInt64(out var reward64))
                                {
                                    result.Rewards.Add((int)Math.Clamp(reward64, int.MinValue, int.MaxValue));
                                }
                            }
                            else
                            {
                                reader.Skip();
                            }
                        }
                    }
                    else
                    {
                        reader.Skip();
                    }
                    break;
                case "total_caught":
                    if (reader.TokenType == JsonTokenType.Number)
                    {
                        if (reader.TryGetInt32(out var total))
                        {
                            result.TotalCaught = total;
                        }
                        else if (reader.TryGetInt64(out var total64))
                        {
                            result.TotalCaught = (int)Math.Clamp(total64, int.MinValue, int.MaxValue);
                        }
                    }
                    else
                    {
                        reader.Skip();
                    }
                    break;
                case "last_caught":
                    result.LastCaught = reader.TokenType == JsonTokenType.String ? reader.GetString() : null;
                    break;
                default:
                    if (reader.TokenType == JsonTokenType.Number)
                    {
                        if (reader.TryGetInt32(out var count))
                        {
                            result.FishCounts[propertyName] = count;
                        }
                        else if (reader.TryGetInt64(out var count64))
                        {
                            result.FishCounts[propertyName] = (int)Math.Clamp(count64, int.MinValue, int.MaxValue);
                        }
                    }
                    else
                    {
                        reader.Skip();
                    }
                    break;
            }
        }

        return result;
    }

    public override void Write(Utf8JsonWriter writer, TrophyFishStats value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("rewards");
        writer.WriteStartArray();
        foreach (var reward in value.Rewards)
        {
            writer.WriteNumberValue(reward);
        }
        writer.WriteEndArray();

        if (value.TotalCaught.HasValue)
        {
            writer.WriteNumber("total_caught", value.TotalCaught.Value);
        }

        if (!string.IsNullOrEmpty(value.LastCaught))
        {
            writer.WriteString("last_caught", value.LastCaught);
        }

        foreach (var kvp in value.FishCounts)
        {
            if (string.Equals(kvp.Key, "rewards", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(kvp.Key, "total_caught", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(kvp.Key, "last_caught", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            writer.WriteNumber(kvp.Key, kvp.Value);
        }

        writer.WriteEndObject();
    }
}
