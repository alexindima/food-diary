using FoodDiary.Modules.Meals.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.ReadModel.Composition.Dashboard;

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
