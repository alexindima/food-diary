namespace FoodDiary.Modules.Dashboard.Contracts.Models;

public sealed record DashboardWaistModel(
    WaistPointModel? Latest,
    WaistPointModel? Previous,
    double? DesiredWaistCm);
