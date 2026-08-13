namespace FoodDiary.Application.Dashboard.Models;

public sealed record DashboardWeightModel(
    WeightPointModel? Latest,
    WeightPointModel? Previous,
    double? Desired);
