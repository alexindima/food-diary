using FoodDiary.Modules.BodyMetrics.Presentation.Contracts.Features.WaistEntries.Responses;

using FoodDiary.Modules.Users.Presentation.Contracts.Responses;

namespace FoodDiary.Modules.BodyMetrics.Presentation.Features.WaistEntries.Responses;

public sealed record WaistHistoryPageSummaryHttpResponse(
    IReadOnlyList<WaistEntryHttpResponse> Entries,
    IReadOnlyList<WaistEntrySummaryHttpResponse> Summary,
    double? HeightCm,
    UserDesiredWaistHttpResponse Goal,
    IReadOnlyList<WaistGoalHistoryHttpResponse> GoalHistory);
