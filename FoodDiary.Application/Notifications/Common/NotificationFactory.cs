using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Notifications.Common;

public static class NotificationFactory {
    public static Notification CreatePasswordSetupSuggested(
        UserId userId,
        string referenceId) =>
        Notification.Create(
            userId,
            NotificationTypes.PasswordSetupSuggested,
            NotificationPayloads.Empty(),
            referenceId);

    public static Notification CreateNewRecommendation(
        UserId userId,
        string dietologistName,
        string? referenceId = null) =>
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
        bool cancelled = false) =>
        Notification.Create(
            userId,
            ResolveClientTaskNotificationType(forDietologist, cancelled),
            NotificationPayloads.Empty(),
            forDietologist ? clientUserId : null);

    private static string ResolveClientTaskNotificationType(bool forDietologist, bool cancelled) {
        if (cancelled) {
            return NotificationTypes.ClientTaskCancelled;
        }

        return forDietologist
            ? NotificationTypes.ClientTaskChangedForDietologist
            : NotificationTypes.NewClientTask;
    }

    public static Notification CreateClientTaskDueSoon(UserId userId) =>
        Notification.Create(
            userId,
            NotificationTypes.ClientTaskDueSoon,
            NotificationPayloads.Empty());

    public static Notification CreateDietologistInvitationReceived(
        UserId userId,
        string clientName,
        string referenceId) =>
        Notification.Create(
            userId,
            NotificationTypes.DietologistInvitationReceived,
            NotificationPayloads.DietologistInvitationReceived(clientName),
            referenceId);

    public static Notification CreateDietologistInvitationAccepted(
        UserId userId,
        string dietologistName,
        string referenceId) =>
        Notification.Create(
            userId,
            NotificationTypes.DietologistInvitationAccepted,
            NotificationPayloads.DietologistInvitationDecision(dietologistName),
            referenceId);

    public static Notification CreateDietologistInvitationDeclined(
        UserId userId,
        string dietologistName,
        string referenceId) =>
        Notification.Create(
            userId,
            NotificationTypes.DietologistInvitationDeclined,
            NotificationPayloads.DietologistInvitationDecision(dietologistName),
            referenceId);

    public static Notification CreateNewComment(
        UserId userId,
        string? referenceId = null) =>
        Notification.Create(
            userId,
            NotificationTypes.NewComment,
            NotificationPayloads.Empty(),
            referenceId);

    public static Notification CreateFastingCompleted(
        UserId userId,
        string planType,
        string occurrenceKind,
        string referenceId) =>
        Notification.Create(
            userId,
            NotificationTypes.FastingCompleted,
            NotificationPayloads.FastingPhase(planType, occurrenceKind),
            referenceId);

    public static Notification CreateEatingWindowStarted(
        UserId userId,
        string planType,
        string occurrenceKind,
        string referenceId) =>
        Notification.Create(
            userId,
            NotificationTypes.EatingWindowStarted,
            NotificationPayloads.FastingPhase(planType, occurrenceKind),
            referenceId);

    public static Notification CreateFastingWindowStarted(
        UserId userId,
        string planType,
        string occurrenceKind,
        string referenceId) =>
        Notification.Create(
            userId,
            NotificationTypes.FastingWindowStarted,
            NotificationPayloads.FastingPhase(planType, occurrenceKind),
            referenceId);

    public static Notification CreateFastingCheckInReminder(
        UserId userId,
        string referenceId) =>
        Notification.Create(
            userId,
            NotificationTypes.FastingCheckInReminder,
            NotificationPayloads.Empty(),
            referenceId);
}
