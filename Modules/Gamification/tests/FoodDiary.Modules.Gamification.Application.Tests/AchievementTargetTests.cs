using FoodDiary.Modules.Gamification.Application.Models;
using FoodDiary.Modules.Gamification.Application.Services;
using FoodDiary.Modules.Gamification.Domain.ValueObjects;

namespace FoodDiary.Modules.Gamification.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class AchievementTargetTests {
    [Fact]
    public void Target_EvaluatesOnlyItsOwnMetric() {
        var metrics = new AchievementMetricSnapshot(2, 100, 10);
        Assert.False(GamificationCalculator.IsTargetReached(AchievementTarget.StreakDays(3), metrics));
        Assert.True(GamificationCalculator.IsTargetReached(AchievementTarget.MealCount(100), metrics));
        Assert.True(GamificationCalculator.IsTargetReached(AchievementTarget.ArticleCount(10), metrics));
        Assert.False(GamificationCalculator.IsTargetReached(AchievementTarget.ArticleCount(int.MaxValue), metrics));
    }
}
