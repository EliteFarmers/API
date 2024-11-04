using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Images;
using Microsoft.AspNetCore.Mvc;

namespace EliteAPI.Models.DTOs.Incoming;

public class CreateBadgeDto {
    [FromForm(Name = "Image"), AllowedFileExtensions]
    public IFormFile? Image { get; set; }
    [FromForm(Name = "Name")]
    public required string Name { get; set; }
    [FromForm(Name = "Description")]
    public required string Description { get; set; }
    [FromForm(Name = "Requirements")]
    public required string Requirements { get; set; }
    [FromForm(Name = "TieToAccount")]
    public bool TieToAccount { get; set; }
}

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

public class EditBadgeDto {
    [FromForm(Name = "Name")]
    public string? Name { get; set; }
    [FromForm(Name = "Description")]
    public string? Description { get; set; }
    [FromForm(Name = "Requirements")]
    public string? Requirements { get; set; }
    [FromForm(Name = "Image"), AllowedFileExtensions]
    public IFormFile? Image { get; set; }
}

public class EditUserBadgeDto {
    public int BadgeId { get; set; }
    public bool? Visible { get; set; }
    public int? Order { get; set; }
}