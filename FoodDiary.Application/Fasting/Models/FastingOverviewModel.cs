using FoodDiary.Application.Common.Models;

namespace FoodDiary.Application.Fasting.Models;

public sealed record FastingOverviewModel(
    FastingSessionModel? CurrentSession,
    FastingStatsModel Stats,
    FastingInsightsModel Insights,
    PagedResponse<FastingSessionModel> History);
