namespace FoodDiary.Modules.Dashboard.Application.Abstractions.Models;

public sealed record DashboardMealsReadModel(
    IReadOnlyList<DashboardMealReadModel> Items,
    int Page,
    int Limit,
    int TotalPages,
    int TotalItems);
