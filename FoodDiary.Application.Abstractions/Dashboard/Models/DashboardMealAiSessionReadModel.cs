namespace FoodDiary.Application.Abstractions.Dashboard.Models;

public sealed record DashboardMealAiSessionReadModel(
    Guid Id,
    Guid MealId,
    Guid? ImageAssetId,
    string? ImageUrl,
    string Source,
    string Status,
    DateTime RecognizedAtUtc,
    string? Notes,
    IReadOnlyList<DashboardMealAiItemReadModel> Items);
