using FoodDiary.Application.WaistEntries.Models;
using FoodDiary.Application.WeightEntries.Models;

namespace FoodDiary.Application.Statistics.Models;

public sealed record StatisticsSummaryModel(
    IReadOnlyList<AggregatedStatisticsModel> Nutrition,
    IReadOnlyList<WeightEntrySummaryModel> Weight,
    IReadOnlyList<WaistEntrySummaryModel> Waist);
