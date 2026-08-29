using FoodDiary.Application.Abstractions.Common.Models;

namespace FoodDiary.Modules.Fasting.Contracts.Read.Models;

public sealed record FastingOverviewModel(
    FastingSessionModel? CurrentSession,
    FastingStatsModel Stats,
    FastingInsightsModel Insights,
    PagedResponse<FastingSessionModel> History);
