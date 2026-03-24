using FoodDiary.Presentation.Api.Features.Consumptions.Responses;

namespace FoodDiary.Presentation.Api.Features.Dashboard.Responses;

public sealed record DashboardMealsHttpResponse(
    IReadOnlyList<ConsumptionHttpResponse> Items,
    int Total);
