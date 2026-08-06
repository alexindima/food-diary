using FoodDiary.Application.Users.Models;

namespace FoodDiary.Application.WeightEntries.Models;

public sealed record WeightHistoryPageSummaryModel(
    IReadOnlyList<WeightEntryModel> Entries,
    IReadOnlyList<WeightEntrySummaryModel> Summary,
    double? Height,
    UserDesiredWeightModel Goal,
    IReadOnlyList<WeightGoalHistoryModel> GoalHistory);
