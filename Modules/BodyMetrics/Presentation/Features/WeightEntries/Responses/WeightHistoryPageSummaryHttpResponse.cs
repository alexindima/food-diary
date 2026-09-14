using FoodDiary.Modules.BodyMetrics.Presentation.Contracts.Features.WeightEntries.Responses;

using FoodDiary.Presentation.Api.Features.Users.Responses;

namespace FoodDiary.Modules.BodyMetrics.Presentation.Features.WeightEntries.Responses;

public sealed record WeightHistoryPageSummaryHttpResponse(
    IReadOnlyList<WeightEntryHttpResponse> Entries,
    IReadOnlyList<WeightEntrySummaryHttpResponse> Summary,
    double? HeightCm,
    UserDesiredWeightHttpResponse Goal,
    IReadOnlyList<WeightGoalHistoryHttpResponse> GoalHistory);
