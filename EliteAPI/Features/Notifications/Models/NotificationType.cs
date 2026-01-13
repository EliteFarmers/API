using EliteAPI.Utilities;

namespace EliteAPI.Features.Notifications.Models;

[JsonStringEnum]
public enum NotificationType
{
    System = 0,
    GuideApproved = 1,
    GuideEditApproved = 2,
    GuideRejected = 3,
    GuideDeleted = 4,
    CommentApproved = 5,
    CommentEditApproved = 6,
    CommentRejected = 7,
    NewComment = 8,
    NewReply = 9,
    ShopPurchase = 10,
    GuideSubmitted = 11
}
