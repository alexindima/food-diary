namespace FoodDiary.Presentation.Api.Features.Fasting.Responses;

public sealed record FastingStatsHttpResponse(
    int TotalCompleted,
    int CurrentStreak,
    double AverageDurationHours,
    double CompletionRateLast30Days,
    double CheckInRateLast30Days,
    DateTime? LastCheckInAtUtc,
    string? TopSymptom);
