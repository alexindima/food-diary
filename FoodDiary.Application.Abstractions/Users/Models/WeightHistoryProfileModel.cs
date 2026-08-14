namespace FoodDiary.Application.Abstractions.Users.Models;

public sealed record WeightHistoryProfileModel(
    double? HeightCm,
    UserDesiredWeightModel Goal,
    IReadOnlyList<WeightGoalHistoryModel> GoalHistory);
