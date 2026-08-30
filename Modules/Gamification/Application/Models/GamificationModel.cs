namespace FoodDiary.Application.Gamification.Models;

public sealed record GamificationModel(
    int CurrentStreak,
    int LongestStreak,
    int TotalMealsLogged,
    int HealthScore,
    double WeeklyAdherence,
    IReadOnlyList<BadgeModel> Badges);
