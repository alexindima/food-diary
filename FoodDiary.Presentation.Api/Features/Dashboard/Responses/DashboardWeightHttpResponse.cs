namespace FoodDiary.Presentation.Api.Features.Dashboard.Responses;

public sealed record DashboardWeightHttpResponse(
    WeightPointHttpResponse? Latest,
    WeightPointHttpResponse? Previous,
    double? Desired);
