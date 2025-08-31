using EliteAPI.Features.Images.Models;

namespace EliteAPI.Models.DTOs.Incoming;

public class BadgeDto {
    public int Id { get; set; }
    public ImageAttachmentDto? Image { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required string Requirements { get; set; }
}

public class UserBadgeDto {
    public int Id { get; set; }
    public required ImageAttachmentDto Image { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required string Requirements { get; set; }
    public required string Timestamp { get; set; }
    public bool Visible { get; set; }
    public int Order { get; set; }
}

public class EditUserBadgeDto {
    public int BadgeId { get; set; }
    public bool? Visible { get; set; }
    public int? Order { get; set; }
}