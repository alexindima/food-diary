using FoodDiary.Application.Abstractions.Users.Models;

namespace FoodDiary.Application.WaistEntries.Models;

public sealed record WaistHistoryPageSummaryModel(
    IReadOnlyList<WaistEntryModel> Entries,
    IReadOnlyList<WaistEntrySummaryModel> Summary,
    double? Height,
    UserDesiredWaistModel Goal,
    IReadOnlyList<WaistGoalHistoryModel> GoalHistory);
