using FoodDiary.Modules.Meals.Service.Contracts.Models;

namespace FoodDiary.Modules.Dashboard.Contracts.Models;

public sealed record DashboardMealsModel(
    IReadOnlyList<MealModel> Items,
    int Total);
