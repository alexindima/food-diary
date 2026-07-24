namespace FoodDiary.Application.Dietologist.Models;

public sealed record BulkRecommendationRecipientResultModel(
    Guid ClientUserId,
    bool Succeeded,
    Guid? RecommendationId,
    bool WasAlreadyProcessed,
    string? ErrorCode);
