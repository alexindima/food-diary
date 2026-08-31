using FoodDiary.Application.Meals.Models;

namespace FoodDiary.Application.Dashboard.Models;

public sealed record DashboardMealsModel(
    IReadOnlyList<MealModel> Items,
    int Total);
