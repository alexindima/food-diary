namespace FoodDiary.Application.Users.Models;

public sealed record WeightHistoryProfileModel(
    double? Height,
    UserDesiredWeightModel Goal,
    IReadOnlyList<WeightGoalHistoryModel> GoalHistory);
