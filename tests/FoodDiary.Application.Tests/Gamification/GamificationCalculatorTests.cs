using FoodDiary.Application.Gamification.Models;
using FoodDiary.Application.Gamification.Services;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Gamification;

[ExcludeFromCodeCoverage]
public class GamificationCalculatorTests {
    private static readonly DateTime Today = new(2026, 4, 6, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void CalculateStreaks_WithNoDates_ReturnsZeros() {
        (int current, int longest) = GamificationCalculator.CalculateStreaks([], Today);

        Assert.Equal(0, current);
        Assert.Equal(0, longest);
    }

    [Fact]
    public void CalculateStreaks_WithTodayOnly_ReturnsOneDay() {
        var dates = new List<DateTime> { Today };

        (int current, int longest) = GamificationCalculator.CalculateStreaks(dates, Today);

        Assert.Equal(1, current);
        Assert.Equal(1, longest);
    }

    [Fact]
    public void CalculateStreaks_WithConsecutiveDaysIncludingToday_CalculatesCorrectly() {
        var dates = new List<DateTime> {
            Today,
            Today.AddDays(-1),
            Today.AddDays(-2),
            Today.AddDays(-3),
        };

        (int current, int longest) = GamificationCalculator.CalculateStreaks(dates, Today);

        Assert.Equal(4, current);
        Assert.Equal(4, longest);
    }

    [Fact]
    public void CalculateStreaks_WithGap_ResetsCurrentButTracksLongest() {
        var dates = new List<DateTime> {
            Today,
            Today.AddDays(-1),
            // gap on -2
            Today.AddDays(-3),
            Today.AddDays(-4),
            Today.AddDays(-5),
        };

        (int current, int longest) = GamificationCalculator.CalculateStreaks(dates, Today);

        Assert.Equal(2, current);
        Assert.Equal(3, longest);
    }

    [Fact]
    public void CalculateStreaks_WithYesterdayStart_CountsAsCurrent() {
        var dates = new List<DateTime> {
            Today.AddDays(-1),
            Today.AddDays(-2),
        };

        (int current, int longest) = GamificationCalculator.CalculateStreaks(dates, Today);

        Assert.Equal(2, current);
        Assert.Equal(2, longest);
    }

    [Fact]
    public void CalculateStreaks_WithOldDatesOnly_CurrentIsZero() {
        var dates = new List<DateTime> {
            Today.AddDays(-5),
            Today.AddDays(-6),
            Today.AddDays(-7),
        };

        (int current, int longest) = GamificationCalculator.CalculateStreaks(dates, Today);

        Assert.Equal(0, current);
        Assert.Equal(3, longest);
    }

    [Fact]
    public void CalculateBadges_WithStreakAndMeals_ReturnCorrectEarnedStatus() {
        IReadOnlyList<BadgeModel> badges = GamificationCalculator.CalculateBadges(longestStreak: 10, totalMeals: 75);

        BadgeModel streak3 = badges.First(b => string.Equals(b.Key, "streak_3", StringComparison.Ordinal));
        BadgeModel streak7 = badges.First(b => string.Equals(b.Key, "streak_7", StringComparison.Ordinal));
        BadgeModel streak14 = badges.First(b => string.Equals(b.Key, "streak_14", StringComparison.Ordinal));
        BadgeModel meals10 = badges.First(b => string.Equals(b.Key, "meals_10", StringComparison.Ordinal));
        BadgeModel meals50 = badges.First(b => string.Equals(b.Key, "meals_50", StringComparison.Ordinal));
        BadgeModel meals100 = badges.First(b => string.Equals(b.Key, "meals_100", StringComparison.Ordinal));

        Assert.True(streak3.IsEarned);
        Assert.True(streak7.IsEarned);
        Assert.False(streak14.IsEarned);
        Assert.True(meals10.IsEarned);
        Assert.True(meals50.IsEarned);
        Assert.False(meals100.IsEarned);
    }

    [Fact]
    public void CalculateBadges_ReturnsAllTenBadges() {
        IReadOnlyList<BadgeModel> badges = GamificationCalculator.CalculateBadges(0, 0);

        Assert.Equal(10, badges.Count);
    }

    [Fact]
    public void CalculateHealthScore_WithPerfectInputs_Returns100() {
        int score = GamificationCalculator.CalculateHealthScore(
            currentStreak: 30, weeklyAdherence: 1.0, totalMeals: 100);

        Assert.Equal(100, score);
    }

    [Fact]
    public void CalculateHealthScore_WithZeroInputs_ReturnsZero() {
        int score = GamificationCalculator.CalculateHealthScore(
            currentStreak: 0, weeklyAdherence: 0, totalMeals: 0);

        Assert.Equal(0, score);
    }

    [Fact]
    public void CalculateHealthScore_CapsStreakAt30Days() {
        int score30 = GamificationCalculator.CalculateHealthScore(30, 0, 0);
        int score100 = GamificationCalculator.CalculateHealthScore(100, 0, 0);

        Assert.Equal(score30, score100);
    }

    [Fact]
    public void CalculateWeeklyAdherence_WithAllDaysWithinRange_Returns1() {
        var userId = UserId.New();
        var meals = new List<Meal>();
        for (int i = 0; i < 7; i++) {
            DateTime date = Today.AddDays(-i);
            var meal = Meal.Create(userId, date, MealType.Lunch);
            meal.ApplyNutrition(new MealNutritionUpdate(2000, 100, 70, 230, 25, 0, IsAutoCalculated: true));
            meals.Add(meal);
        }

        double adherence = GamificationCalculator.CalculateWeeklyAdherence(
            meals, _ => 2000, Today);

        Assert.Equal(1.0, adherence);
    }

    [Fact]
    public void CalculateWeeklyAdherence_WithNoGoal_AndMealsExist_Returns1() {
        var userId = UserId.New();
        var meal = Meal.Create(userId, Today, MealType.Lunch);
        meal.ApplyNutrition(new MealNutritionUpdate(2000, 100, 70, 230, 25, 0, IsAutoCalculated: true));

        double adherence = GamificationCalculator.CalculateWeeklyAdherence(
            [meal], _ => null, Today);

        Assert.Equal(1.0, adherence);
    }

    [Fact]
    public void CalculateWeeklyAdherence_WithNoMealsAndNoGoal_Returns0() {
        double adherence = GamificationCalculator.CalculateWeeklyAdherence(
            [], _ => null, Today);

        Assert.Equal(0.0, adherence);
    }

    [Fact]
    public void CalculateWeeklyAdherence_WithGoalButNoPositiveCalories_DoesNotCountDayAsMet() {
        var userId = UserId.New();
        var meal = Meal.Create(userId, Today, MealType.Lunch);

        double adherence = GamificationCalculator.CalculateWeeklyAdherence(
            [meal], _ => 2000, Today);

        Assert.Equal(0.0, adherence);
    }
}
