namespace FoodDiary.Modules.Dietologist.Presentation.Requests;

public sealed record BulkCreateRecommendationsHttpRequest(
    IReadOnlyList<Guid> ClientUserIds,
    string Text,
    string IdempotencyKey);
