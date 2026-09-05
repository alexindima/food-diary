namespace FoodDiary.Presentation.Api.Features.Dietologist.Responses;

public sealed record BulkRecommendationRecipientResultHttpResponse(
    Guid ClientUserId,
    bool Succeeded,
    Guid? RecommendationId,
    bool WasAlreadyProcessed,
    string? ErrorCode);
