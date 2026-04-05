namespace FoodDiary.Presentation.Api.Features.Gamification.Responses;

public sealed record GamificationHttpResponse(
    int CurrentStreak,
    int LongestStreak,
    int TotalMealsLogged,
    int HealthScore,
    double WeeklyAdherence,
    IReadOnlyList<BadgeHttpResponse> Badges);

public sealed record BadgeHttpResponse(
    string Key,
    string Category,
    int Threshold,
    bool IsEarned);
