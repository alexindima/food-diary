namespace FoodDiary.Application.Users.Models;

public sealed record WaistHistoryProfileModel(
    double? Height,
    UserDesiredWaistModel Goal,
    IReadOnlyList<WaistGoalHistoryModel> GoalHistory);
