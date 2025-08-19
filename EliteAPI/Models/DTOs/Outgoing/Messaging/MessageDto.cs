namespace EliteAPI.Models.DTOs.Outgoing.Messaging; 

public class MessageDto {
    public required string Name { get; init; }
    public required string GuildId { get; init; }
    public required string AuthorId { get; init; }
    public required Dictionary<string, object> Data { get; init; }
}