using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Dietologist.Common;

internal static class DietologistNotificationFactory {
    public static Notification CreateNewRecommendation(UserId userId, string dietologistName, string? referenceId = null) =>
        Notification.Create(
            userId,
            NotificationTypes.NewRecommendation,
            NotificationPayloads.NewRecommendation(dietologistName),
            referenceId);

    public static Notification CreateNewRecommendationComment(
        UserId userId,
        string recommendationId,
        string clientUserId,
        bool forDietologist) =>
        Notification.Create(
            userId,
            forDietologist
                ? NotificationTypes.NewRecommendationCommentForDietologist
                : NotificationTypes.NewRecommendationComment,
            NotificationPayloads.Empty(),
            forDietologist ? $"{clientUserId}|{recommendationId}" : recommendationId);

    public static Notification CreateClientTaskChanged(
        UserId userId,
        string clientUserId,
        bool forDietologist,
        bool cancelled = false) {
        string notificationType = ResolveClientTaskNotificationType(forDietologist, cancelled);
        return Notification.Create(
            userId,
            notificationType,
            NotificationPayloads.Empty(),
            forDietologist ? clientUserId : null);
    }

    private static string ResolveClientTaskNotificationType(bool forDietologist, bool cancelled) {
        if (cancelled) {
            return NotificationTypes.ClientTaskCancelled;
        }

        return forDietologist
            ? NotificationTypes.ClientTaskChangedForDietologist
            : NotificationTypes.NewClientTask;
    }

    public static Notification CreateClientTaskDueSoon(UserId userId) =>
        Notification.Create(userId, NotificationTypes.ClientTaskDueSoon, NotificationPayloads.Empty());

    public static Notification CreateInvitationReceived(UserId userId, string clientName, string referenceId) =>
        Notification.Create(
            userId,
            NotificationTypes.DietologistInvitationReceived,
            NotificationPayloads.DietologistInvitationReceived(clientName),
            referenceId);

    public static Notification CreateInvitationAccepted(UserId userId, string dietologistName, string referenceId) =>
        Notification.Create(
            userId,
            NotificationTypes.DietologistInvitationAccepted,
            NotificationPayloads.DietologistInvitationDecision(dietologistName),
            referenceId);

    public static Notification CreateInvitationDeclined(UserId userId, string dietologistName, string referenceId) =>
        Notification.Create(
            userId,
            NotificationTypes.DietologistInvitationDeclined,
            NotificationPayloads.DietologistInvitationDecision(dietologistName),
            referenceId);
}
