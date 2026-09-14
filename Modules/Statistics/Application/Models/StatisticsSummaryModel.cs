using FoodDiary.Modules.BodyMetrics.Contracts.WaistEntries.Models;
using FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Models;

namespace FoodDiary.Application.Statistics.Models;

public sealed record StatisticsSummaryModel(
    IReadOnlyList<AggregatedStatisticsModel> Nutrition,
    IReadOnlyList<WeightEntrySummaryModel> Weight,
    IReadOnlyList<WaistEntrySummaryModel> Waist);
