namespace FoodDiary.Modules.Dietologist.Application.Models;

public sealed record BulkRecommendationResultModel(
    string IdempotencyKey,
    IReadOnlyList<BulkRecommendationRecipientResultModel> Recipients);
