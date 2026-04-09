using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Notifications.Common;

public static class NotificationFactory {
    public static Notification CreateNewRecommendation(
        UserId userId,
        string dietologistName,
        string? referenceId = null) =>
        Notification.Create(
            userId,
            NotificationTypes.NewRecommendation,
            NotificationPayloads.NewRecommendation(dietologistName),
            referenceId);

    public static Notification CreateNewComment(
        UserId userId,
        string? referenceId = null) =>
        Notification.Create(
            userId,
            NotificationTypes.NewComment,
            NotificationPayloads.Empty(),
            referenceId);
}
