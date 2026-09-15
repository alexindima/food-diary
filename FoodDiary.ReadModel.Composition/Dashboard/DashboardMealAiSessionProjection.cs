using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Dashboard.Infrastructure.Persistence;

internal sealed record DashboardMealAiSessionProjection(
    MealId MealId,
    MealAiSessionId SessionId,
    Guid Id,
    Guid MealIdValue,
    Guid? ImageAssetId,
    string? ImageUrl,
    string Source,
    string Status,
    DateTime RecognizedAtUtc,
    string? Notes);
