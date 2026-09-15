namespace FoodDiary.Modules.Dashboard.Presentation.Contracts.Responses;

public sealed record DashboardWeightHttpResponse(
    WeightPointHttpResponse? Latest,
    WeightPointHttpResponse? Previous,
    double? DesiredWeightKg);
