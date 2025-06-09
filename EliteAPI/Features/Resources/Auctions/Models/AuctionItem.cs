using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using EliteAPI.Features.Resources.Auctions.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EliteAPI.Features.Resources.Auctions.Models;

public class AuctionItem
{
    [Key]
    [Column(Order = 0)]
    public required string SkyblockId { get; set; }

    [Key]
    [Column(Order = 1)]
    public required string VariantKey { get; set; }

    public decimal? Lowest { get; set; }
    public int LowestVolume { get; set; }
    public DateTimeOffset? LowestObservedAt { get; set; }

    public decimal? Lowest3Day { get; set; }
    public int Lowest3DayVolume { get; set; }

    public decimal? Lowest7Day { get; set; }
    public int Lowest7DayVolume { get; set; }

    public DateTimeOffset CalculatedAt { get; set; }
}

public class AuctionItemVariation {
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Rarity { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Dictionary<string, int>? Enchantments { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Pet { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public PetLevelGroup? PetLevel { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Dictionary<string, string>? ItemAttributes { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Dictionary<string, string>? Extra { get; set; }
    
    public string ToKey() {
        var list = new List<string>();
        if (Rarity is not null) {
            list.Add("r:" + Rarity);
        }
        
        if (Pet is not null) {
            list.Add($"pet:{Pet}");
        }
        
        if (PetLevel is not null) {
            if (PetLevel.Min > 0) {
                list.Add($"pet_group:{PetLevel.Min}{(PetLevel.Max > PetLevel.Min ? "-" + PetLevel.Max : "")}");
            }
        }
        
        if (Enchantments is not null) {
            list.AddRange(Enchantments.Select(enchantment => $"en:{enchantment.Key}={enchantment.Value}"));
        }
        
        if (ItemAttributes is not null) {
            list.AddRange(ItemAttributes.Select(attribute => $"at:{attribute.Key}={attribute.Value}"));
        }
        
        if (Extra is not null) {
            list.AddRange(Extra.Select(extra => $"ex:{extra.Key}={extra.Value}"));
        }
        
        return string.Join(VariantKeyGenerator.JoinSeparator, list);
    }

    public static AuctionItemVariation FromKey(string variationKey) {
        var parts = variationKey.Split(VariantKeyGenerator.JoinSeparator);
        var variation = new AuctionItemVariation();
        
        foreach (var part in parts) {
            var sections = part.Split(':', 2);
            if (sections.Length < 2) continue;
            
            var key = sections[0];
            var value = sections[1];

            switch (key) {
                case "r":
                    variation.Rarity = value;
                    break;
                case "pet":
                    variation.Pet = value;
                    break;
                case "pet_k":
                    variation.PetLevel ??= new PetLevelGroup { Key = value };
                    break;
                case "pet_group":
                    var groupParts = value.Split('-');
                    if (groupParts.Length == 2 && int.TryParse(groupParts[0], out var min) && int.TryParse(groupParts[1], out var max)) {
                        variation.PetLevel ??= new PetLevelGroup { Key = "LVL_" + min + "-" + max, Min = min, Max = max };
                    } else if (int.TryParse(groupParts[0], out var singleLevel)) {
                        variation.PetLevel ??= new PetLevelGroup { Key = "LVL_" + singleLevel, Min = singleLevel, Max = singleLevel };
                    }
                    break;
                case "en":
                    var enchantParts = value.Split('=');
                    if (enchantParts.Length == 2) {
                        variation.Enchantments ??= new Dictionary<string, int>();
                        variation.Enchantments[enchantParts[0]] = int.Parse(enchantParts[1]);
                    }
                    break;
                case "at":
                    var attrParts = value.Split('=');
                    if (attrParts.Length == 2) {
                        variation.ItemAttributes ??= new Dictionary<string, string>();
                        variation.ItemAttributes[attrParts[0]] = attrParts[1];
                    }
                    break;
                case "ex":
                    var extraParts = value.Split('=');
                    if (extraParts.Length == 2) {
                        variation.Extra ??= new Dictionary<string, string>();
                        variation.Extra[extraParts[0]] = extraParts[1];
                    }
                    break;
            }
        }

        return variation;
    }
    
    public class PetLevelGroup
    {
        public required string Key { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int Min { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int Max { get; set; }
    }
}

public class AuctionItemVariantSummaryConfiguration : IEntityTypeConfiguration<AuctionItem>
{
    public void Configure(EntityTypeBuilder<AuctionItem> builder)
    {
        builder.HasKey(e => new { e.SkyblockId, e.VariantKey });
        builder.HasIndex(e => e.CalculatedAt);
    }
}