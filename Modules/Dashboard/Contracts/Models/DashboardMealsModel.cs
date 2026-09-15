using FoodDiary.Application.Meals.Models;

namespace FoodDiary.Modules.Dashboard.Contracts.Models;

public sealed record DashboardMealsModel(
    IReadOnlyList<MealModel> Items,
    int Total);
