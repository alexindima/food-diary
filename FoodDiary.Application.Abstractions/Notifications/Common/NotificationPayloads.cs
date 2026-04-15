namespace FoodDiary.Application.Notifications.Common;

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

public sealed record EmptyNotificationPayload;

public sealed record NewRecommendationNotificationPayload(string DietologistName);

public sealed record DietologistInvitationReceivedNotificationPayload(string ClientName);

public sealed record DietologistInvitationDecisionNotificationPayload(string DietologistName);

public sealed record FastingPhaseNotificationPayload(string PlanType, string OccurrenceKind);
