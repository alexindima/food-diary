using FoodDiary.Presentation.Api.Features.Users.Responses;

namespace FoodDiary.Presentation.Api.Features.WeightEntries.Responses;

public sealed record WeightHistoryPageSummaryHttpResponse(
    IReadOnlyList<WeightEntryHttpResponse> Entries,
    IReadOnlyList<WeightEntrySummaryHttpResponse> Summary,
    double? HeightCm,
    UserDesiredWeightHttpResponse Goal,
    IReadOnlyList<WeightGoalHistoryHttpResponse> GoalHistory);
