namespace EliteAPI.Models.DTOs.Incoming;

public class CreateBadgeDto {
    public required string ImageId { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required string Requirements { get; set; }
}

public class BadgeDto {
    public int Id { get; set; }
    public required string ImageId { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required string Requirements { get; set; }
}

public class UserBadgeDto {
    public int Id { get; set; }
    public required string ImageId { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required string Requirements { get; set; }
    public required string Timestamp { get; set; }
    public bool Visible { get; set; }
    public int Order { get; set; }
}

public class EditBadgeDto {
    public string? ImageId { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Requirements { get; set; }
}

public class EditUserBadgeDto {
    public int BadgeId { get; set; }
    public bool? Visible { get; set; }
    public int? Order { get; set; }
}