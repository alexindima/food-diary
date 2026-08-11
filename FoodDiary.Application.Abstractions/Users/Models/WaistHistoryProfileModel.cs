namespace FoodDiary.Application.Abstractions.Users.Models;

public sealed record WaistHistoryProfileModel(
    double? Height,
    UserDesiredWaistModel Goal,
    IReadOnlyList<WaistGoalHistoryModel> GoalHistory);
