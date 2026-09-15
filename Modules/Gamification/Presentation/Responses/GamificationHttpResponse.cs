namespace FoodDiary.Modules.Gamification.Presentation.Responses;

public sealed record GamificationHttpResponse(
    int CurrentStreak,
    int LongestStreak,
    int TotalMealsLogged,
    int HealthScore,
    double WeeklyAdherence,
    IReadOnlyList<BadgeHttpResponse> Badges);
