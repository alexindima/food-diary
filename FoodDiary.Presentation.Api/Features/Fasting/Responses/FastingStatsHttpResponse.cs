namespace FoodDiary.Presentation.Api.Features.Fasting.Responses;

public sealed record FastingStatsHttpResponse(int TotalCompleted, int CurrentStreak, double AverageDurationHours);
