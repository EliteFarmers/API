namespace EliteAPI.Models.DTOs.Incoming; 

public class IncomingAccountDto {
    public required ulong Id { get; set; }
    public required string Username { get; set; }
    public string? DisplayName { get; set; }
    public string? Discriminator { get; set; }
    public string? Avatar { get; set; }
    public string? Locale { get; set; }
    public string? Banner { get; set; }
}
