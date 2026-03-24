namespace FoodDiary.Presentation.Api.Features.Dashboard.Responses;

public sealed record DashboardWaistHttpResponse(
    WaistPointHttpResponse? Latest,
    WaistPointHttpResponse? Previous,
    double? Desired);
