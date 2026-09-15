using FoodDiary.Modules.BodyMetrics.Presentation.Contracts.Features.WaistEntries.Responses;
using FoodDiary.Modules.BodyMetrics.Presentation.Contracts.Features.WeightEntries.Responses;

namespace FoodDiary.Modules.Statistics.Presentation.Responses;

public sealed record StatisticsSummaryHttpResponse(
    IReadOnlyList<AggregatedStatisticsHttpResponse> Nutrition,
    IReadOnlyList<WeightEntrySummaryHttpResponse> Weight,
    IReadOnlyList<WaistEntrySummaryHttpResponse> Waist);
