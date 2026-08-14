using FoodDiary.Application.Abstractions.Users.Models;

using FoodDiary.Application.Abstractions.WeightEntries.Models;

namespace FoodDiary.Application.BodyMetrics.WeightEntries.Models;

public sealed record WeightHistoryPageSummaryModel(
    IReadOnlyList<WeightEntryModel> Entries,
    IReadOnlyList<WeightEntrySummaryModel> Summary,
    double? Height,
    UserDesiredWeightModel Goal,
    IReadOnlyList<WeightGoalHistoryModel> GoalHistory);
