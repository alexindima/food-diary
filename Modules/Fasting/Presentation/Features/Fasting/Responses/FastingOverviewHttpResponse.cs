using FoodDiary.Presentation.Api.Responses;

namespace FoodDiary.Presentation.Api.Features.Fasting.Responses;

public sealed record FastingOverviewHttpResponse(
    FastingSessionHttpResponse? CurrentSession,
    FastingStatsHttpResponse Stats,
    FastingInsightsHttpResponse Insights,
    PagedHttpResponse<FastingSessionHttpResponse> History);
