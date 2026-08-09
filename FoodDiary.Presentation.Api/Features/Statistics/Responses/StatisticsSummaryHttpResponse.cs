using FoodDiary.Presentation.Api.Features.WaistEntries.Responses;
using FoodDiary.Presentation.Api.Features.WeightEntries.Responses;

namespace FoodDiary.Presentation.Api.Features.Statistics.Responses;

public sealed record StatisticsSummaryHttpResponse(
    IReadOnlyList<AggregatedStatisticsHttpResponse> Nutrition,
    IReadOnlyList<WeightEntrySummaryHttpResponse> Weight,
    IReadOnlyList<WaistEntrySummaryHttpResponse> Waist);
