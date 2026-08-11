namespace FoodDiary.Application.Abstractions.Users.Models;

public sealed record WeightHistoryProfileModel(
    double? Height,
    UserDesiredWeightModel Goal,
    IReadOnlyList<WeightGoalHistoryModel> GoalHistory);
