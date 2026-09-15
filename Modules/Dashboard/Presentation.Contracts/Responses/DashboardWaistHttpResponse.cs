namespace FoodDiary.Modules.Dashboard.Presentation.Contracts.Responses;

public sealed record DashboardWaistHttpResponse(
    WaistPointHttpResponse? Latest,
    WaistPointHttpResponse? Previous,
    double? DesiredWaistCm);
