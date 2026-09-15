namespace FoodDiary.Modules.Dietologist.Application.Abstractions.Models;

public sealed record RecommendationBulkDispatchReadModel(
    Guid ClientUserId,
    Guid RecommendationId);
