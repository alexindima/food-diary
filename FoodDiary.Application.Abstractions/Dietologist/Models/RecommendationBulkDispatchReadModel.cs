namespace FoodDiary.Application.Abstractions.Dietologist.Models;

public sealed record RecommendationBulkDispatchReadModel(
    Guid ClientUserId,
    Guid RecommendationId);
