namespace FoodDiary.Application.Gamification.Models;

public sealed record AchievementMetricSnapshot(
    int LongestStreak,
    int TotalMeals,
    int TotalAcademyArticlesRead);
