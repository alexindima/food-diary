namespace FoodDiary.Application.Abstractions.Notifications.Common;

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
        string[] parts = referenceId.Split('|', 2, StringSplitOptions.TrimEntries);
        return parts.Length == 2 && Guid.TryParse(parts[0], out _) && Guid.TryParse(parts[1], out _)
            ? $"/dietologist/clients/{parts[0]}?recommendationId={parts[1]}"
            : null;
    }
}
