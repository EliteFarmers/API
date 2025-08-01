﻿using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using EliteAPI.Features.Monetization.Models;
using EliteAPI.Models.DTOs.Incoming;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Monetization;
using Swashbuckle.AspNetCore.Annotations;

namespace EliteAPI.Features.Account.DTOs;

[SwaggerSchema(Required = ["Id", "DisplayName", "Username", "Settings", "MinecraftAccounts"])]
public class AuthorizedAccountDto
{
    /// <summary>
    /// Discord user ID
    /// </summary>
    public required string Id { get; set; }
    /// <summary>
    /// Discord display name
    /// </summary>
    public required string DisplayName { get; set; }
    /// <summary>
    /// Discord username (unique)
    /// </summary>
    public required string Username { get; set; }
    
    [Obsolete("Discriminator is deprecated and will be removed in a future version.")]
    public string? Discriminator { get; set; }
    
    /// <summary>
    /// Discord user locale
    /// </summary>
    public string? Locale { get; set; }
    /// <summary>
    /// Discord avatar URL hash
    /// </summary>
    public string? Avatar { get; set; }
    /// <summary>
    /// Discord banner URL hash
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Banner { get; set; }
    
    public UserSettingsDto Settings { get; set; } = new();
    
    /// <summary>
    /// Purchased entitlements from the Discord store
    /// </summary>
    public List<EntitlementDto> Entitlements { get; set; } = [];
    
    /// <summary>
    /// Linked Minecraft accounts
    /// </summary>
    public List<MinecraftAccountDetailsDto> MinecraftAccounts { get; set; } = [];
    
    /// <summary>
    /// Dismissed announcements by the user
    /// </summary>
    public List<string> DismissedAnnouncements { get; set; } = [];
}

[SwaggerSchema(Required = ["Id", "Name", "Properties", "PrimaryAccount"])]
public class MinecraftAccountDetailsDto
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public bool PrimaryAccount { get; set; } = true;
    public List<UserBadgeDto> Badges { get; set; } = new();
    public MinecraftSkinDto Skin { get; set; } = new();
}

[SwaggerSchema(Required = ["Id", "Name", "FormattedName", "Properties", "Profiles", "PrimaryAccount", "EventEntries"])]
public class MinecraftAccountDto
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string FormattedName { get; set; }
    public bool PrimaryAccount { get; set; } = true;
    
    public string? DiscordId { get; set; }
    public string? DiscordUsername { get; set; }
    public string? DiscordAvatar { get; set; }
    public UserSettingsDto Settings { get; set; } = new();
    public MinecraftSkinDto Skin { get; set; } = new();
    public List<ProfileDetailsDto> Profiles { get; set; } = new();
    public List<UserBadgeDto> Badges { get; set; } = new();
    public PlayerDataDto? PlayerData { get; set; }
}

[SwaggerSchema(Required = new [] { "Name", "Value" })]
public class MinecraftAccountPropertyDto
{
    public required string Name { get; set; }
    public required string Value { get; set; }
}

public class MinecraftSkinDto
{
    /// <summary>
    /// Minecraft skin texture ID
    /// </summary>
    public string? Texture { get; set; }
    
    /// <summary>
    /// Base64 data image of the 8x8 face
    /// </summary>
    public string? Face { get; set; }
    
    /// <summary>
    /// Base64 data image of the 8x8 hat (overlay on the face)
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Hat { get; set; }
}


public class UserSettingsDto
{
    /// <summary>
    /// Custom name prefix
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Prefix { get; set; }
    /// <summary>
    /// Custom name suffix
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Suffix { get; set; }
    
    /// <summary>
    /// Configurated features for the user
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ConfiguredProductFeaturesDto? Features { get; set; }
    
    /// <summary>
    /// Selected weight style for the user
    /// </summary>
    /// 
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public WeightStyleLinkedDto? WeightStyle { get; set; }
    
    /// <summary>
    /// Selected leaderboard style for the user
    /// </summary>
    /// 
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public WeightStyleLinkedDto? LeaderboardStyle { get; set; }
    
    /// <summary>
    /// Selected name style for the user
    /// </summary>
    /// 
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public WeightStyleLinkedDto? NameStyle { get; set; }
    
    /// <summary>
    /// Fortune settings for the user
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FortuneSettingsDto? Fortune { get; set; }
}

public class UpdateUserSettingsDto
{
    /// <summary>
    /// Custom name prefix
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Prefix { get; set; }
    
    /// <summary>
    /// Custom name suffix
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Suffix { get; set; }
    
    /// <summary>
    /// Configurated features for the user
    /// </summary>
    public ConfiguredProductFeaturesDto? Features { get; set; }
    
    /// <summary>
    /// Selected weight style for the user
    /// </summary>
    public int? WeightStyleId { get; set; }
    
    /// <summary>
    /// Selected leaderboard style for the user
    /// </summary>
    public int? LeaderboardStyleId { get; set; }
        
    /// <summary>
    /// Selected name style for the user
    /// </summary>
    public int? NameStyleId { get; set; }
}

public class ConfiguredProductFeaturesDto {
    /// <summary>
    /// Name of weight style to use.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int? WeightStyle { get; set; }
    
    /// <summary>
    /// Ability to override other's weight styles.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool? WeightStyleOverride { get; set; }
    
    /// <summary>
    /// Embed color for the bot.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [MaxLength(6)]
    public string? EmbedColor { get; set; }
    
    /// <summary>
    /// Show "More Info" on weight command by default.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool? MoreInfoDefault { get; set; }
    
    /// <summary>
    /// If shop promotions should be hidden.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool? HideShopPromotions { get; set; }
    
    /// <summary>
    /// Custom name emoji URL.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    [MaxLength(256)]
    public string? EmojiUrl { get; set; }
}

public class UnlockedProductFeaturesDto
{
    /// <summary>
    /// ID of unlocked badge.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int? BadgeId { get; set; }
    /// <summary>
    /// Name of weight style to unlock.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public List<string>? WeightStyles { get; set; }
    /// <summary>
    /// Ability to override other's weight styles.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool? WeightStyleOverride { get; set; }
    /// <summary>
    /// Embed color for the bot.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public List<string>? EmbedColors { get; set; }
    /// <summary>
    /// Ability to hide shop promotions.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool? HideShopPromotions { get; set; }
    /// <summary>
    /// Show "More Info" on weight command by default.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool? MoreInfoDefault { get; set; }
    /// <summary>
    /// Ability to have custom name emoji for the user.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool? CustomEmoji { get; set; }
    /// <summary>
    /// Maximum number of events that can be created in a month. (For guilds)
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int? MaxMonthlyEvents { get; set; }
    /// <summary>
    /// Maximum number of jacob leaderboard that can be active at once. (For guilds)
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int? MaxJacobLeaderboards { get; set; }
}

public class LinkedAccountsDto {
    public string? SelectedUuid { get; set; }
    public List<PlayerDataDto> Players { get; set; } = new();
}

public class AccountWithPermsDto {
    public required string Id { get; set; }
    public required string DisplayName { get; set; }
    public required string Username { get; set; }
    public List<string> Roles { get; set; } = new();

    public string? Discriminator { get; set; }
    public string? Avatar { get; set; }
}

public class EntitlementDto {
    /// <summary>
    /// Entitlement ID
    /// </summary>
    public required string Id { get; set; }
    /// <summary>
    /// Type of entitlement
    /// </summary>
    public EntitlementType Type { get; set; }
    /// <summary>
    /// Target of entitlement.
    /// 0 = None
    /// 1 = User
    /// 2 = Guild
    /// </summary>
    public EntitlementTarget Target { get; set; } = EntitlementTarget.None;
    
    /// <summary>
    /// SKU ID of the product
    /// </summary>
    public required string ProductId { get; set; }
    
    /// <summary>
    /// Product details
    /// </summary>
    public required ProductDto Product { get; set; }
	
    public bool Deleted { get; set; }
    
    /// <summary>
    /// Consumed status of the entitlement if applicable
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Consumed { get; set; }
	
    /// <summary>
    /// Start date of the entitlement
    /// </summary>
    public DateTimeOffset? StartDate { get; set; }
    
    /// <summary>
    /// End date of the entitlement
    /// </summary>
    public DateTimeOffset? EndDate { get; set; }
}

public class ProductDto {
    /// <summary>
    /// Product ID
    /// </summary>
    public required string Id { get; set; }
    
    /// <summary>
    /// Product name
    /// </summary>
    public required string Name { get; set; }
    
    /// <summary>
    /// Slug of the product
    /// </summary>
    public required string Slug { get; set; }
    
    /// <summary>
    /// Product price
    /// </summary>
    public int Price { get; set; }
    
    /// <summary>
    /// Product description
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// If the product is available for purchase
    /// </summary>
    public bool Available { get; set; }
    
    /// <summary>
    /// Type of product
    /// </summary>
    public ProductType Type { get; set; }
    
    /// <summary>
    /// Features of the product
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public UnlockedProductFeaturesDto Features { get; set; } = new();
    
    /// <summary>
    /// Unlocked weight styles
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public List<WeightStyleLinkedDto> WeightStyles { get; set; } = [];
    
    /// <summary>
    /// Product thumbnail
    /// </summary>
    public ImageAttachmentDto? Thumbnail { get; set; }
    
    /// <summary>
    /// Product Images
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public List<ImageAttachmentDto> Images { get; set; } = [];
    
    /// <summary>
    /// Discord flags
    /// </summary>
    public int Flags { get; set; }

    public bool IsSubscription { get; set; }
    public bool IsGuildSubscription { get; set; }
    public bool IsUserSubscription { get; set; }
}

public class EditProductDto {
    /// <summary>
    /// Description of the product
    /// </summary>
    [MaxLength(1024)]
    public string? Description { get; set; }
    
    /// <summary>
    /// If the product is available for purchase
    /// </summary>
    public bool? Available { get; set; }
    
    /// <summary>
    /// Product price in USD cents
    /// </summary>
    public int? Price { get; set; }
    
    /// <summary>
    /// Features of the product
    /// </summary>
    public UnlockedProductFeaturesDto? Features { get; set; }
    
    /// <summary>
    /// Unix seconds timestamp of release date
    /// </summary>
    public string? ReleasedAt { get; set; }
}