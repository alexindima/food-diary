namespace FoodDiary.Presentation.Api.Features.Dietologist.Responses;

public sealed record BulkRecommendationResultHttpResponse(
    string IdempotencyKey,
    IReadOnlyList<BulkRecommendationRecipientResultHttpResponse> Recipients);
