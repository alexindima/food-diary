namespace FoodDiary.Application.Abstractions.Notifications.Common;

public static class NotificationTargetUrlResolver {
    public static string? Resolve(string notificationType, string? referenceId = null) {
        return notificationType switch {
            NotificationTypes.PasswordSetupSuggested => "/profile?intent=set-password",
            NotificationTypes.FastingCheckInReminder => "/fasting?intent=check-in",
            NotificationTypes.FastingCompleted => "/fasting?intent=session-complete",
            NotificationTypes.FastingWindowStarted => "/fasting?intent=fasting-window",
            NotificationTypes.EatingWindowStarted => "/fasting?intent=eating-window",
            NotificationTypes.DietologistInvitationReceived when !string.IsNullOrWhiteSpace(referenceId) =>
                $"/dietologist-invitations/{referenceId}",
            NotificationTypes.DietologistInvitationAccepted => "/profile",
            NotificationTypes.DietologistInvitationDeclined => "/profile",
            _ => null
        };
    }
}
