using FoodDiary.Application.Consumptions.Models;

namespace FoodDiary.Application.Dashboard.Models;

public sealed record DashboardMealsModel(
    IReadOnlyList<ConsumptionModel> Items,
    int Total);
