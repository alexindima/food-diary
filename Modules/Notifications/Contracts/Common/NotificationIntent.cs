namespace FoodDiary.Modules.Notifications.Contracts.Common;

public sealed record NotificationIntent {
    public string Type { get; }
    public string PayloadJson { get; }
    public string? ReferenceId { get; }
    public bool IsLegacy { get; }

    private NotificationIntent(string type, string payloadJson, string? referenceId, bool isLegacy = false) {
        Type = type;
        PayloadJson = payloadJson;
        ReferenceId = referenceId;
        IsLegacy = isLegacy;
    }

    public static NotificationIntent PasswordSetupSuggested(string referenceId) => new(NotificationTypes.PasswordSetupSuggested, NotificationPayloads.Empty(), referenceId);
    public static NotificationIntent NewRecommendation(NewRecommendationNotificationPayload payload, string? referenceId = null) =>
        new(NotificationTypes.NewRecommendation, NotificationPayloadSerializer.Serialize(payload), referenceId);
    public static NotificationIntent RecommendationComment(RecommendationCommentTarget target) {
        ArgumentNullException.ThrowIfNull(target);
        return new NotificationIntent(target.ClientUserId is null ? NotificationTypes.NewRecommendationComment : NotificationTypes.NewRecommendationCommentForDietologist,
            NotificationPayloads.Empty(), target.ToReference());
    }
    public static NotificationIntent NewClientTask() => new(NotificationTypes.NewClientTask, NotificationPayloads.Empty(), referenceId: null);
    public static NotificationIntent ClientTaskChangedForDietologist(string clientUserId) => new(NotificationTypes.ClientTaskChangedForDietologist, NotificationPayloads.Empty(), clientUserId);
    public static NotificationIntent ClientTaskCancelled(string? referenceId = null) => new(NotificationTypes.ClientTaskCancelled, NotificationPayloads.Empty(), referenceId);
    public static NotificationIntent ClientTaskDueSoon() => new(NotificationTypes.ClientTaskDueSoon, NotificationPayloads.Empty(), referenceId: null);
    public static NotificationIntent WeeklyGoalReminder(string referenceId) => new(NotificationTypes.WeeklyGoalReminder, NotificationPayloads.Empty(), referenceId);
    public static NotificationIntent InvitationReceived(DietologistInvitationReceivedNotificationPayload payload, string referenceId) =>
        new(NotificationTypes.DietologistInvitationReceived, NotificationPayloadSerializer.Serialize(payload), referenceId);
    public static NotificationIntent InvitationAccepted(DietologistInvitationDecisionNotificationPayload payload, string referenceId) =>
        new(NotificationTypes.DietologistInvitationAccepted, NotificationPayloadSerializer.Serialize(payload), referenceId);
    public static NotificationIntent InvitationDeclined(DietologistInvitationDecisionNotificationPayload payload, string referenceId) =>
        new(NotificationTypes.DietologistInvitationDeclined, NotificationPayloadSerializer.Serialize(payload), referenceId);
    public static NotificationIntent NewComment(string? referenceId = null) => new(NotificationTypes.NewComment, NotificationPayloads.Empty(), referenceId);
    public static NotificationIntent FastingCompleted(FastingPhaseNotificationPayload payload, string referenceId) =>
        new(NotificationTypes.FastingCompleted, NotificationPayloadSerializer.Serialize(payload), referenceId);
    public static NotificationIntent EatingWindowStarted(FastingPhaseNotificationPayload payload, string referenceId) =>
        new(NotificationTypes.EatingWindowStarted, NotificationPayloadSerializer.Serialize(payload), referenceId);
    public static NotificationIntent FastingWindowStarted(FastingPhaseNotificationPayload payload, string referenceId) =>
        new(NotificationTypes.FastingWindowStarted, NotificationPayloadSerializer.Serialize(payload), referenceId);
    public static NotificationIntent FastingCheckInReminder(string referenceId) => new(NotificationTypes.FastingCheckInReminder, NotificationPayloads.Empty(), referenceId);

    // Boundary for existing requests/versioned data: retain unknown types and JSON verbatim.
    public static NotificationIntent FromLegacy(string type, string payloadJson, string? referenceId = null) => new(type, payloadJson, referenceId, isLegacy: true);
}
