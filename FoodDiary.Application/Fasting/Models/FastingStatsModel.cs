namespace FoodDiary.Application.Fasting.Models;

public sealed record FastingStatsModel(
    int TotalCompleted,
    int CurrentStreak,
    double AverageDurationHours);
