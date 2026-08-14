namespace FoodDiary.Application.Abstractions.Users.Models;

public sealed record WaistHistoryProfileModel(
    double? HeightCm,
    UserDesiredWaistModel Goal,
    IReadOnlyList<WaistGoalHistoryModel> GoalHistory);
