namespace FoodDiary.Modules.Dashboard.Contracts.Models;

public sealed record DashboardWeightModel(
    WeightPointModel? Latest,
    WeightPointModel? Previous,
    double? DesiredWeightKg);
