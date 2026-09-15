using FoodDiary.Modules.Fasting.Presentation.Contracts.Responses;
using FoodDiary.Presentation.Api.Responses;

namespace FoodDiary.Modules.Fasting.Presentation.Responses;

public sealed record FastingOverviewHttpResponse(
    FastingSessionHttpResponse? CurrentSession,
    FastingStatsHttpResponse Stats,
    FastingInsightsHttpResponse Insights,
    PagedHttpResponse<FastingSessionHttpResponse> History);
