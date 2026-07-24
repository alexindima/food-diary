namespace FoodDiary.Application.Dietologist.Models;

public sealed record BulkRecommendationResultModel(
    string IdempotencyKey,
    IReadOnlyList<BulkRecommendationRecipientResultModel> Recipients);
