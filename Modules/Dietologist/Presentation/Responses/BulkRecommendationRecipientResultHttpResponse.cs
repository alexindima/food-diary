namespace FoodDiary.Modules.Dietologist.Presentation.Responses;

public sealed record BulkRecommendationRecipientResultHttpResponse(
    Guid ClientUserId,
    bool Succeeded,
    Guid? RecommendationId,
    bool WasAlreadyProcessed,
    string? ErrorCode);
