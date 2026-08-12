using FoodDiary.Application.Abstractions.Common.Models;

namespace FoodDiary.Application.Abstractions.Fasting.Models;

public sealed record FastingOverviewModel(
    FastingSessionModel? CurrentSession,
    FastingStatsModel Stats,
    FastingInsightsModel Insights,
    PagedResponse<FastingSessionModel> History);
