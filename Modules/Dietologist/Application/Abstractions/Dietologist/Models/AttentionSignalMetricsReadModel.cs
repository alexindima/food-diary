namespace FoodDiary.Application.Abstractions.Dietologist.Models;

public sealed record AttentionSignalMetricsReadModel(
    Guid ClientUserId,
    double DailyCalorieTarget,
    DateTime? LastMealAtUtc,
    IReadOnlyList<AttentionSignalDailyCaloriesReadModel> DailyCalories,
    IReadOnlyList<AttentionSignalWeightPointReadModel> WeightPoints);
