using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Dietologist.Application.Common;

internal static class DietologistNotificationFactory {
    public static NotificationRequest CreateNewRecommendation(UserId userId, string dietologistName, string? referenceId = null) =>
        new(
            userId,
            NotificationTypes.NewRecommendation,
            NotificationPayloads.NewRecommendation(dietologistName),
            referenceId);

    public static NotificationRequest CreateNewRecommendationComment(
        UserId userId,
        string recommendationId,
        string clientUserId,
        bool forDietologist) =>
        new(
            userId,
            forDietologist
                ? NotificationTypes.NewRecommendationCommentForDietologist
                : NotificationTypes.NewRecommendationComment,
            NotificationPayloads.Empty(),
            forDietologist ? $"{clientUserId}|{recommendationId}" : recommendationId);

    public static NotificationRequest CreateClientTaskChanged(
        UserId userId,
        string clientUserId,
        bool forDietologist,
        bool cancelled = false) {
        string notificationType = ResolveClientTaskNotificationType(forDietologist, cancelled);
        return new NotificationRequest(
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

    public static NotificationRequest CreateClientTaskDueSoon(UserId userId) =>
        new(userId, NotificationTypes.ClientTaskDueSoon, NotificationPayloads.Empty());

    public static NotificationRequest CreateInvitationReceived(UserId userId, string clientName, string referenceId) =>
        new(
            userId,
            NotificationTypes.DietologistInvitationReceived,
            NotificationPayloads.DietologistInvitationReceived(clientName),
            referenceId);

    public static NotificationRequest CreateInvitationAccepted(UserId userId, string dietologistName, string referenceId) =>
        new(
            userId,
            NotificationTypes.DietologistInvitationAccepted,
            NotificationPayloads.DietologistInvitationDecision(dietologistName),
            referenceId);

    public static NotificationRequest CreateInvitationDeclined(UserId userId, string dietologistName, string referenceId) =>
        new(
            userId,
            NotificationTypes.DietologistInvitationDeclined,
            NotificationPayloads.DietologistInvitationDecision(dietologistName),
            referenceId);
}
