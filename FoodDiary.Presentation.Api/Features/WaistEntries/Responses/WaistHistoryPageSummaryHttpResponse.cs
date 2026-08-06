using FoodDiary.Presentation.Api.Features.Users.Responses;

namespace FoodDiary.Presentation.Api.Features.WaistEntries.Responses;

public sealed record WaistHistoryPageSummaryHttpResponse(
    IReadOnlyList<WaistEntryHttpResponse> Entries,
    IReadOnlyList<WaistEntrySummaryHttpResponse> Summary,
    double? Height,
    UserDesiredWaistHttpResponse Goal,
    IReadOnlyList<WaistGoalHistoryHttpResponse> GoalHistory);
