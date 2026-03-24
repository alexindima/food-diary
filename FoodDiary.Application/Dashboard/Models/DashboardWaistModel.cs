namespace FoodDiary.Application.Dashboard.Models;

public sealed record DashboardWaistModel(
    WaistPointModel? Latest,
    WaistPointModel? Previous,
    double? Desired);
