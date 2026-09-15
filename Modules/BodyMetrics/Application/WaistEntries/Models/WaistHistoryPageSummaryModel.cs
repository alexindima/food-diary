using FoodDiary.Modules.Users.Contracts.Models;

using FoodDiary.Modules.BodyMetrics.Contracts.WaistEntries.Models;

namespace FoodDiary.Modules.BodyMetrics.Application.WaistEntries.Models;

public sealed record WaistHistoryPageSummaryModel(
    IReadOnlyList<WaistEntryModel> Entries,
    IReadOnlyList<WaistEntrySummaryModel> Summary,
    double? HeightCm,
    UserDesiredWaistModel Goal,
    IReadOnlyList<WaistGoalHistoryModel> GoalHistory);
