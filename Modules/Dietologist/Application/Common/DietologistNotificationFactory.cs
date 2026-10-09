using FoodDiary.Modules.Notifications.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Dietologist.Application.Common;

internal static class DietologistNotificationFactory {
    public static NotificationRequest CreatePasswordSetupSuggested(UserId userId, string referenceId) =>
        new(userId, NotificationIntent.PasswordSetupSuggested(referenceId));
    public static NotificationRequest CreateNewRecommendation(UserId userId, string dietologistName, string? referenceId = null) =>
        new(userId, NotificationIntent.NewRecommendation(new NewRecommendationNotificationPayload(dietologistName), referenceId));
    public static NotificationRequest CreateNewRecommendationComment(UserId userId, string recommendationId, string clientUserId, bool forDietologist) =>
        new(userId, NotificationIntent.RecommendationComment(forDietologist
            ? RecommendationCommentTarget.ForDietologist(recommendationId, clientUserId)
            : RecommendationCommentTarget.ForRecipient(recommendationId)));
    public static NotificationRequest CreateClientTaskChanged(UserId userId, string clientUserId, bool forDietologist, bool cancelled = false) {
        NotificationIntent intent;
        if (cancelled) {
            intent = NotificationIntent.ClientTaskCancelled(forDietologist ? clientUserId : null);
        } else {
            intent = forDietologist ? NotificationIntent.ClientTaskChangedForDietologist(clientUserId) : NotificationIntent.NewClientTask();
        }
        return new NotificationRequest(userId, intent);
    }
    public static NotificationRequest CreateClientTaskDueSoon(UserId userId) => new(userId, NotificationIntent.ClientTaskDueSoon());
    public static NotificationRequest CreateWeeklyGoalReminder(UserId userId, string referenceId) => new(userId, NotificationIntent.WeeklyGoalReminder(referenceId));
    public static NotificationRequest CreateInvitationReceived(UserId userId, string clientName, string referenceId) =>
        new(userId, NotificationIntent.InvitationReceived(new DietologistInvitationReceivedNotificationPayload(clientName), referenceId));
    public static NotificationRequest CreateInvitationAccepted(UserId userId, string dietologistName, string referenceId) =>
        new(userId, NotificationIntent.InvitationAccepted(new DietologistInvitationDecisionNotificationPayload(dietologistName), referenceId));
    public static NotificationRequest CreateInvitationDeclined(UserId userId, string dietologistName, string referenceId) =>
        new(userId, NotificationIntent.InvitationDeclined(new DietologistInvitationDecisionNotificationPayload(dietologistName), referenceId));
    public static NotificationRequest CreateNewComment(UserId userId, string? referenceId = null) => new(userId, NotificationIntent.NewComment(referenceId));
    public static NotificationRequest CreateFastingCompleted(UserId userId, string planType, string occurrenceKind, string referenceId) =>
        new(userId, NotificationIntent.FastingCompleted(new FastingPhaseNotificationPayload(planType, occurrenceKind), referenceId));
    public static NotificationRequest CreateEatingWindowStarted(UserId userId, string planType, string occurrenceKind, string referenceId) =>
        new(userId, NotificationIntent.EatingWindowStarted(new FastingPhaseNotificationPayload(planType, occurrenceKind), referenceId));
    public static NotificationRequest CreateFastingWindowStarted(UserId userId, string planType, string occurrenceKind, string referenceId) =>
        new(userId, NotificationIntent.FastingWindowStarted(new FastingPhaseNotificationPayload(planType, occurrenceKind), referenceId));
    public static NotificationRequest CreateFastingCheckInReminder(UserId userId, string referenceId) => new(userId, NotificationIntent.FastingCheckInReminder(referenceId));
}
