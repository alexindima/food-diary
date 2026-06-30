namespace FoodDiary.Application.Abstractions.Dashboard.Models;

public sealed record DashboardMealsReadModel(
    IReadOnlyList<DashboardMealReadModel> Items,
    int Page,
    int Limit,
    int TotalPages,
    int TotalItems);
