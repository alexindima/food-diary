namespace FoodDiary.Modules.Fasting.Contracts.Read.Models;

public sealed record FastingStatsModel(
    int TotalCompleted,
    int CurrentStreak,
    double AverageDurationHours,
    double CompletionRateLast30Days,
    double CheckInRateLast30Days,
    DateTime? LastCheckInAtUtc,
    string? TopSymptom);
