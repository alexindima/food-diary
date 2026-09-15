using FoodDiary.Modules.Users.Contracts.Models;

using FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Models;

namespace FoodDiary.Modules.BodyMetrics.Application.WeightEntries.Models;

public sealed record WeightHistoryPageSummaryModel(
    IReadOnlyList<WeightEntryModel> Entries,
    IReadOnlyList<WeightEntrySummaryModel> Summary,
    double? HeightCm,
    UserDesiredWeightModel Goal,
    IReadOnlyList<WeightGoalHistoryModel> GoalHistory);
