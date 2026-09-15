namespace FoodDiary.Modules.Users.Contracts.Models;

public sealed record WaistHistoryProfileModel(
    double? HeightCm,
    UserDesiredWaistModel Goal,
    IReadOnlyList<WaistGoalHistoryModel> GoalHistory);
