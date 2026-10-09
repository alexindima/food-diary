using FoodDiary.Modules.Notifications.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

using FoodDiary.Modules.Notifications.Domain.Entities;

namespace FoodDiary.Modules.Notifications.Application.Common;

public static class NotificationFactory {
    public static Notification CreatePasswordSetupSuggested(UserId userId, string referenceId) =>
        Create(userId, NotificationIntent.PasswordSetupSuggested(referenceId));
    public static Notification CreateNewRecommendation(UserId userId, string dietologistName, string? referenceId = null) =>
        Create(userId, NotificationIntent.NewRecommendation(new NewRecommendationNotificationPayload(dietologistName), referenceId));
    public static Notification CreateNewRecommendationComment(UserId userId, string recommendationId, string clientUserId, bool forDietologist) =>
        Create(userId, NotificationIntent.RecommendationComment(forDietologist
            ? RecommendationCommentTarget.ForDietologist(recommendationId, clientUserId)
            : RecommendationCommentTarget.ForRecipient(recommendationId)));
    public static Notification CreateClientTaskChanged(UserId userId, string clientUserId, bool forDietologist, bool cancelled = false) {
        NotificationIntent intent;
        if (cancelled) {
            intent = NotificationIntent.ClientTaskCancelled(forDietologist ? clientUserId : null);
        } else {
            intent = forDietologist ? NotificationIntent.ClientTaskChangedForDietologist(clientUserId) : NotificationIntent.NewClientTask();
        }
        return Create(userId, intent);
    }
    public static Notification CreateClientTaskDueSoon(UserId userId) => Create(userId, NotificationIntent.ClientTaskDueSoon());
    public static Notification CreateWeeklyGoalReminder(UserId userId, string referenceId) => Create(userId, NotificationIntent.WeeklyGoalReminder(referenceId));
    public static Notification CreateDietologistInvitationReceived(UserId userId, string clientName, string referenceId) =>
        Create(userId, NotificationIntent.InvitationReceived(new DietologistInvitationReceivedNotificationPayload(clientName), referenceId));
    public static Notification CreateDietologistInvitationAccepted(UserId userId, string dietologistName, string referenceId) =>
        Create(userId, NotificationIntent.InvitationAccepted(new DietologistInvitationDecisionNotificationPayload(dietologistName), referenceId));
    public static Notification CreateDietologistInvitationDeclined(UserId userId, string dietologistName, string referenceId) =>
        Create(userId, NotificationIntent.InvitationDeclined(new DietologistInvitationDecisionNotificationPayload(dietologistName), referenceId));
    public static Notification CreateNewComment(UserId userId, string? referenceId = null) => Create(userId, NotificationIntent.NewComment(referenceId));
    public static Notification CreateFastingCompleted(UserId userId, string planType, string occurrenceKind, string referenceId) =>
        Create(userId, NotificationIntent.FastingCompleted(new FastingPhaseNotificationPayload(planType, occurrenceKind), referenceId));
    public static Notification CreateEatingWindowStarted(UserId userId, string planType, string occurrenceKind, string referenceId) =>
        Create(userId, NotificationIntent.EatingWindowStarted(new FastingPhaseNotificationPayload(planType, occurrenceKind), referenceId));
    public static Notification CreateFastingWindowStarted(UserId userId, string planType, string occurrenceKind, string referenceId) =>
        Create(userId, NotificationIntent.FastingWindowStarted(new FastingPhaseNotificationPayload(planType, occurrenceKind), referenceId));
    public static Notification CreateFastingCheckInReminder(UserId userId, string referenceId) => Create(userId, NotificationIntent.FastingCheckInReminder(referenceId));

    private static Notification Create(UserId userId, NotificationIntent intent) =>
        Notification.Create(userId, intent.Type, intent.PayloadJson, intent.ReferenceId);
}
