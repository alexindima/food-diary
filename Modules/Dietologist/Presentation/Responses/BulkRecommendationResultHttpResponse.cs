namespace FoodDiary.Modules.Dietologist.Presentation.Responses;

public sealed record BulkRecommendationResultHttpResponse(
    string IdempotencyKey,
    IReadOnlyList<BulkRecommendationRecipientResultHttpResponse> Recipients);
