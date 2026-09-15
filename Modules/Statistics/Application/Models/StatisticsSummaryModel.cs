using FoodDiary.Modules.BodyMetrics.Contracts.WaistEntries.Models;
using FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Models;

namespace FoodDiary.Modules.Statistics.Application.Models;

public sealed record StatisticsSummaryModel(
    IReadOnlyList<AggregatedStatisticsModel> Nutrition,
    IReadOnlyList<WeightEntrySummaryModel> Weight,
    IReadOnlyList<WaistEntrySummaryModel> Waist);
