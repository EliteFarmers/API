using System.Text.Json.Serialization;

namespace HypixelAPI.DTOs;

public class AuctionHouseResponse
{
    public bool Success { get; set; }
    public int Page { get; set; }
    public int TotalPages { get; set; }
    public int TotalAuctions { get; set; }
    public long LastUpdated { get; set; }
    public List<AuctionResponse> Auctions { get; set; } = [];
}

public class AuctionResponse
{
    [JsonPropertyName("uuid")]
    public required string Uuid { get; set; }
    
    [JsonPropertyName("auctioneer")]
    public required string Auctioneer { get; set; }
    
    [JsonPropertyName("profile_id")]
    public required string ProfileId { get; set; }
    
    [JsonPropertyName("coop")]
    public string[]? Coop { get; set; }
    
    [JsonPropertyName("start")]
    public long Start { get; set; }
    
    [JsonPropertyName("end")]
    public long End { get; set; }
    
    [JsonPropertyName("item_name")]
    public required string ItemName { get; set; }
    
    [JsonPropertyName("item_lore")]
    public string? ItemLore { get; set; }
    
    [JsonPropertyName("extra")]
    public string? Extra { get; set; }
    
    [JsonPropertyName("categories")]
    public string[]? Categories { get; set; }
    
    [JsonPropertyName("category")]
    public string? Category { get; set; }
    
    [JsonPropertyName("tier")]
    public string? Tier { get; set; }
    
    [JsonPropertyName("starting_bid")]
    public long StartingBid { get; set; }
    
    [JsonPropertyName("item_bytes")]
    public required string ItemBytes { get; set; }
    
    // [JsonPropertyName("claimed")]
    // public bool Claimed { get; set; }
    //
    // [JsonPropertyName("claimed_bidders")]
    // public object[] ClaimedBidders { get; set; }
    
    [JsonPropertyName("highest_bid_amount")]
    public long HighestBidAmount { get; set; }
    
    [JsonPropertyName("last_updated")]
    public long LastUpdated { get; set; }
    
    [JsonPropertyName("bin")]
    public bool Bin { get; set; }
    
    [JsonPropertyName("bids")]
    public List<AuctionBidResponse> Bids { get; set; } = [];
    
    [JsonPropertyName("item_uuid")]
    public string? ItemUuid { get; set; }
    
}

public class AuctionBidResponse
{
    [JsonPropertyName("auction_id")]
    public required string AuctionId { get; set; }
    
    [JsonPropertyName("bidder")]
    public required string Bidder { get; set; }
    
    [JsonPropertyName("profile_id")]
    public required string ProfileId { get; set; }
    
    [JsonPropertyName("amount")]
    public long Amount { get; set; }
    
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }
}

