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

    public static Notification CreateDietologistInvitationReceived(
        UserId userId,
        string clientName,
        string referenceId) =>
        Notification.Create(
            userId,
            NotificationTypes.DietologistInvitationReceived,
            NotificationPayloads.DietologistInvitationReceived(clientName),
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
