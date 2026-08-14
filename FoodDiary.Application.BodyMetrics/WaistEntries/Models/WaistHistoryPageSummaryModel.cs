using FoodDiary.Application.Abstractions.Users.Models;

using FoodDiary.Application.Abstractions.WaistEntries.Models;

namespace FoodDiary.Application.BodyMetrics.WaistEntries.Models;

public sealed record WaistHistoryPageSummaryModel(
    IReadOnlyList<WaistEntryModel> Entries,
    IReadOnlyList<WaistEntrySummaryModel> Summary,
    double? Height,
    UserDesiredWaistModel Goal,
    IReadOnlyList<WaistGoalHistoryModel> GoalHistory);
