using FoodDiary.Modules.Gamification.Domain.Contracts.Enums;
using FoodDiary.Modules.Gamification.Domain.ValueObjects;

namespace FoodDiary.Modules.Gamification.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class AchievementTargetValidationTests {
    [Theory]
    [InlineData((AchievementMetric)(-1))]
    [InlineData((AchievementMetric)999)]
    public void UnknownMetric_CannotBecomeAnAchievementTarget(AchievementMetric metric) {
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => AchievementTarget.FromFields(metric, 1));

        Assert.Equal("metric", error.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NonpositiveThreshold_IsRejected(int threshold) {
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => AchievementTarget.FromFields(AchievementMetric.TotalMeals, threshold));

        Assert.Equal("threshold", error.ParamName);
    }

    [Fact]
    public void Target_PreservesTheMaximumPositiveThreshold() {
        var target = AchievementTarget.FromFields(AchievementMetric.TotalMeals, int.MaxValue);

        Assert.Equal(AchievementMetric.TotalMeals, target.Metric);
        Assert.Equal(int.MaxValue, target.Threshold);
        Assert.Equal(AchievementTarget.MealCount(int.MaxValue), target);
    }
}
