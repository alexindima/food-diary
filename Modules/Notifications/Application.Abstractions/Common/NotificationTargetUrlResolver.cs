using FoodDiary.Modules.Notifications.Contracts.Common;
namespace FoodDiary.Modules.Notifications.Application.Abstractions.Common;

public static class NotificationTargetUrlResolver {
    public static string? Resolve(string notificationType, string? referenceId = null) {
        return notificationType switch {
            NotificationTypes.PasswordSetupSuggested => "/profile?intent=set-password",
            NotificationTypes.FastingCheckInReminder => "/fasting?intent=check-in",
            NotificationTypes.FastingCompleted => "/fasting?intent=session-complete",
            NotificationTypes.FastingWindowStarted => "/fasting?intent=fasting-window",
            NotificationTypes.EatingWindowStarted => "/fasting?intent=eating-window",
            NotificationTypes.NewRecommendation when !string.IsNullOrWhiteSpace(referenceId) =>
                $"/recommendations?recommendationId={referenceId}",
            NotificationTypes.NewRecommendation => "/recommendations",
            NotificationTypes.NewRecommendationComment when !string.IsNullOrWhiteSpace(referenceId) =>
                $"/recommendations?recommendationId={referenceId}",
            NotificationTypes.NewRecommendationComment => "/recommendations",
            NotificationTypes.NewRecommendationCommentForDietologist when !string.IsNullOrWhiteSpace(referenceId) =>
                ResolveDietologistRecommendationCommentUrl(referenceId),
            NotificationTypes.NewClientTask => "/recommendations",
            NotificationTypes.ClientTaskCancelled => "/recommendations",
            NotificationTypes.ClientTaskDueSoon => "/recommendations",
            NotificationTypes.WeeklyGoalReminder => "/weekly-check-in",
            NotificationTypes.ClientTaskChangedForDietologist when !string.IsNullOrWhiteSpace(referenceId) =>
                $"/dietologist/clients/{referenceId}",
            NotificationTypes.DietologistInvitationReceived when !string.IsNullOrWhiteSpace(referenceId) =>
                $"/dietologist-invitations/{referenceId}",
            NotificationTypes.DietologistInvitationAccepted => "/profile",
            NotificationTypes.DietologistInvitationDeclined => "/profile",
            _ => null,
        };
    }

    private static string? ResolveDietologistRecommendationCommentUrl(string referenceId) {
        var target = RecommendationCommentTarget.ParseDietologistReference(referenceId);
        return target is not null
            ? $"/dietologist/clients/{target.ClientUserId}?recommendationId={target.RecommendationId}"
            : null;
    }
}
