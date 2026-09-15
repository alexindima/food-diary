namespace FoodDiary.Modules.Users.Contracts.Models;

public sealed record WeightHistoryProfileModel(
    double? HeightCm,
    UserDesiredWeightModel Goal,
    IReadOnlyList<WeightGoalHistoryModel> GoalHistory);
