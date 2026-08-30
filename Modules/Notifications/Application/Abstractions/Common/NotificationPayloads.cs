namespace FoodDiary.Application.Abstractions.Notifications.Common;

public static class NotificationPayloads {
    public static string Empty() => NotificationPayloadSerializer.Serialize(new EmptyNotificationPayload());

    public static string NewRecommendation(string dietologistName) =>
        NotificationPayloadSerializer.Serialize(new NewRecommendationNotificationPayload(dietologistName));

    public static string DietologistInvitationReceived(string clientName) =>
        NotificationPayloadSerializer.Serialize(new DietologistInvitationReceivedNotificationPayload(clientName));

    public static string DietologistInvitationDecision(string dietologistName) =>
        NotificationPayloadSerializer.Serialize(new DietologistInvitationDecisionNotificationPayload(dietologistName));

    public static string FastingPhase(string planType, string occurrenceKind) =>
        NotificationPayloadSerializer.Serialize(new FastingPhaseNotificationPayload(planType, occurrenceKind));
}
