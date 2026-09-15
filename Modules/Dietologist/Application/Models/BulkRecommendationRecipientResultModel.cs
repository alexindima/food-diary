namespace FoodDiary.Modules.Dietologist.Application.Models;

public sealed record BulkRecommendationRecipientResultModel(
    Guid ClientUserId,
    bool Succeeded,
    Guid? RecommendationId,
    bool WasAlreadyProcessed,
    string? ErrorCode);
