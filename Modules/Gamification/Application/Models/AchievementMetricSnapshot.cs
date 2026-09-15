namespace FoodDiary.Modules.Gamification.Application.Models;

public sealed record AchievementMetricSnapshot(
    int LongestStreak,
    int TotalMeals,
    int TotalAcademyArticlesRead);
