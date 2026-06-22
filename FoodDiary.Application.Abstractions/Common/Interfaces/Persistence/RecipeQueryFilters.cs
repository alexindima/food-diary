namespace FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;

public sealed record RecipeQueryFilters(
    string? Search,
    string? Category = null,
    int? MaxTotalTime = null,
    double? CaloriesFrom = null,
    double? CaloriesTo = null,
    bool? HasImage = null);
