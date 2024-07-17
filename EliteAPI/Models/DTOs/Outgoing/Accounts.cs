using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using EliteAPI.Models.DTOs.Incoming;
using EliteAPI.Models.Entities.Monetization;
using Swashbuckle.AspNetCore.Annotations;

namespace EliteAPI.Models.DTOs.Outgoing;

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
    /// Discord email, not asked for normally
    /// </summary>
    public string? Email { get; set; }
    /// <summary>
    /// Discord user locale
    /// </summary>
    public string? Locale { get; set; }
    /// <summary>
    /// Discord avatar URL hash
    /// </summary>
    public string? Avatar { get; set; }
    
    public UserSettingsDto Settings { get; set; } = new();
    /// <summary>
    /// Purchased entitlements from the Discord store
    /// </summary>
    public List<EntitlementDto> Entitlements { get; set; } = [];
    /// <summary>
    /// Linked Minecraft accounts
    /// </summary>
    public List<MinecraftAccountDetailsDto> MinecraftAccounts { get; set; } = [];
}

[SwaggerSchema(Required = ["Id", "Name", "Properties", "PrimaryAccount"])]
public class MinecraftAccountDetailsDto
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public bool PrimaryAccount { get; set; } = true;
    public List<UserBadgeDto> Badges { get; set; } = new();
    public List<MinecraftAccountPropertyDto> Properties { get; set; } = new();
}

[SwaggerSchema(Required = ["Id", "Name", "Properties", "Profiles", "PrimaryAccount", "EventEntries"])]
public class MinecraftAccountDto
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public bool PrimaryAccount { get; set; } = true;
    
    public string? DiscordId { get; set; }
    public string? DiscordUsername { get; set; }
    public string? DiscordAvatar { get; set; }
    public UserSettingsDto Settings { get; set; } = new();

    public List<MinecraftAccountPropertyDto> Properties { get; set; } = new();
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


public class UserSettingsDto
{
    /// <summary>
    /// Configurated features for the user
    /// </summary>
    public ConfiguredProductFeaturesDto? Features { get; set; }
}

public class ConfiguredProductFeaturesDto {
    /// <summary>
    /// Name of weight style to use.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? WeightStyle { get; set; }
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
    [MaxLength(6)]
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
    /// Icon URL
    /// </summary>
    public string? Icon { get; set; }
    
    /// <summary>
    /// Product description
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Type of product
    /// </summary>
    public ProductType Type { get; set; }
    
    /// <summary>
    /// Category of the product
    /// </summary>
    public ProductCategory Category { get; set; } = ProductCategory.None;
    
    /// <summary>
    /// Features of the product
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public UnlockedProductFeaturesDto Features { get; set; } = new();
    
    /// <summary>
    /// Discord flags
    /// </summary>
    public int Flags { get; set; }
}

public class UpdateProductDto {
    public ProductCategory? Category { get; set; }
    [MaxLength(256)]
    public string? Icon { get; set; }
    [MaxLength(1024)]
    public string? Description { get; set; }
    
    public UnlockedProductFeaturesDto? Features { get; set; }
}