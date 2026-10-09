namespace FoodDiary.Modules.Notifications.Contracts.Common;

public sealed record RecommendationCommentTarget {
    public string RecommendationId { get; }
    public string? ClientUserId { get; }

    private RecommendationCommentTarget(string recommendationId, string? clientUserId) {
        RecommendationId = recommendationId;
        ClientUserId = clientUserId;
    }

    public static RecommendationCommentTarget ForRecipient(string recommendationId) => new(recommendationId, clientUserId: null);
    public static RecommendationCommentTarget ForDietologist(string recommendationId, string clientUserId) => new(recommendationId, clientUserId);
    public string ToReference() => ClientUserId is null ? RecommendationId : $"{ClientUserId}|{RecommendationId}";

    public static RecommendationCommentTarget? ParseDietologistReference(string reference) {
        string[] parts = reference.Split('|', 2, StringSplitOptions.TrimEntries);
        return parts.Length == 2 && Guid.TryParse(parts[0], out _) && Guid.TryParse(parts[1], out _)
            ? ForDietologist(parts[1], parts[0])
            : null;
    }
}
