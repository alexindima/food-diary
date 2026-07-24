namespace FoodDiary.Presentation.Api.Features.Dietologist.Requests;

public sealed record BulkCreateRecommendationsHttpRequest(
    IReadOnlyList<Guid> ClientUserIds,
    string Text,
    string IdempotencyKey);
