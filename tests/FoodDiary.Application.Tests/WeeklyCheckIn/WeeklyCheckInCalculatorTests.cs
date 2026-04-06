using FoodDiary.Application.WeeklyCheckIn.Models;
using FoodDiary.Application.WeeklyCheckIn.Services;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.WeeklyCheckIn;

public class WeeklyCheckInCalculatorTests {
    private static readonly UserId TestUserId = UserId.New();
    private static readonly DateTime WeekStart = new(2026, 3, 30, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void BuildSummary_WithEmptyData_ReturnsZeros() {
        var summary = WeeklyCheckInCalculator.BuildSummary(
            [], [], [], [], daysInPeriod: 7);

        Assert.Equal(0, summary.TotalCalories);
        Assert.Equal(0, summary.AvgDailyCalories);
        Assert.Equal(0, summary.MealsLogged);
        Assert.Equal(0, summary.DaysLogged);
        Assert.Null(summary.WeightStart);
        Assert.Null(summary.WaistStart);
    }

    [Fact]
    public void BuildSummary_WithMeals_CalculatesAveragesCorrectly() {
        var meals = new List<Meal>();
        for (var i = 0; i < 3; i++) {
            var meal = Meal.Create(TestUserId, WeekStart.AddDays(i), MealType.Lunch);
            meal.ApplyNutrition(new MealNutritionUpdate(700, 40, 25, 80, 8, 0, true));
            meals.Add(meal);
        }

        var summary = WeeklyCheckInCalculator.BuildSummary(
            meals, [], [], [], daysInPeriod: 7);

        Assert.Equal(2100, summary.TotalCalories);
        Assert.Equal(300, summary.AvgDailyCalories);
        Assert.Equal(3, summary.MealsLogged);
        Assert.Equal(3, summary.DaysLogged);
    }

    [Fact]
    public void BuildSummary_WithWeightsAndWaists_TracksStartAndEnd() {
        var weights = new List<WeightEntry> {
            WeightEntry.Create(TestUserId, WeekStart, 80),
            WeightEntry.Create(TestUserId, WeekStart.AddDays(6), 79.5),
        };
        var waists = new List<WaistEntry> {
            WaistEntry.Create(TestUserId, WeekStart, 85),
            WaistEntry.Create(TestUserId, WeekStart.AddDays(6), 84),
        };

        var summary = WeeklyCheckInCalculator.BuildSummary(
            [], weights, waists, [], daysInPeriod: 7);

        Assert.Equal(80, summary.WeightStart);
        Assert.Equal(79.5, summary.WeightEnd);
        Assert.Equal(85, summary.WaistStart);
        Assert.Equal(84, summary.WaistEnd);
    }

    [Fact]
    public void BuildSummary_WithHydration_CalculatesTotal() {
        var hydration = new List<(DateTime Date, int TotalMl)> {
            (WeekStart, 2000),
            (WeekStart.AddDays(1), 1800),
        };

        var summary = WeeklyCheckInCalculator.BuildSummary(
            [], [], [], hydration, daysInPeriod: 7);

        Assert.Equal(3800, summary.TotalHydrationMl);
        Assert.Equal(542, summary.AvgDailyHydrationMl);
    }

    [Fact]
    public void BuildTrends_CalculatesDifferences() {
        var thisWeek = new WeekSummaryModel(14000, 2000, 120, 70, 230, 21, 7, null, 79.5, null, 84, 14000, 2000);
        var lastWeek = new WeekSummaryModel(12000, 1714.3, 100, 65, 200, 18, 6, null, 80, null, 85, 12000, 1714);

        var trends = WeeklyCheckInCalculator.BuildTrends(thisWeek, lastWeek);

        Assert.Equal(285.7, trends.CalorieChange);
        Assert.Equal(20, trends.ProteinChange);
        Assert.Equal(-0.5, trends.WeightChange);
        Assert.Equal(-1, trends.WaistChange);
        Assert.Equal(3, trends.MealsLoggedChange);
    }

    [Fact]
    public void BuildTrends_WithNullWeights_ReturnsNullChanges() {
        var thisWeek = new WeekSummaryModel(14000, 2000, 120, 70, 230, 21, 7, null, null, null, null, 0, 0);
        var lastWeek = new WeekSummaryModel(12000, 1714, 100, 65, 200, 18, 6, null, 80, null, 85, 0, 0);

        var trends = WeeklyCheckInCalculator.BuildTrends(thisWeek, lastWeek);

        Assert.Null(trends.WeightChange);
        Assert.Null(trends.WaistChange);
    }

    [Fact]
    public void GenerateSuggestions_WhenFewDaysLogged_SuggestsLogMore() {
        var summary = new WeekSummaryModel(6000, 1500, 80, 50, 180, 6, 3, null, null, null, null, 0, 0);
        var trends = new WeekTrendModel(0, 0, 0, 0, null, null, 0, 0);

        var suggestions = WeeklyCheckInCalculator.GenerateSuggestions(summary, trends, 2000);

        Assert.Contains("suggestion.log_more_days", suggestions);
    }

    [Fact]
    public void GenerateSuggestions_WhenOverCalorieGoal_SuggestsOver() {
        var summary = new WeekSummaryModel(17500, 2500, 130, 90, 280, 21, 7, null, null, null, null, 0, 0);
        var trends = new WeekTrendModel(0, 0, 0, 0, null, null, 0, 0);

        var suggestions = WeeklyCheckInCalculator.GenerateSuggestions(summary, trends, 2000);

        Assert.Contains("suggestion.over_calorie_goal", suggestions);
    }

    [Fact]
    public void GenerateSuggestions_WhenOnTrack_SuggestsOnTrack() {
        var summary = new WeekSummaryModel(14000, 2000, 120, 70, 230, 21, 7, null, null, null, null, 14000, 2000);
        var trends = new WeekTrendModel(0, 0, 0, 0, 0, 0, 0, 0);

        var suggestions = WeeklyCheckInCalculator.GenerateSuggestions(summary, trends, 2000);

        Assert.Contains("suggestion.on_track", suggestions);
    }

    [Fact]
    public void GenerateSuggestions_WhenWeightIncreasing_SuggestsWeightIncreasing() {
        var summary = new WeekSummaryModel(14000, 2000, 120, 70, 230, 21, 7, null, null, null, null, 0, 0);
        var trends = new WeekTrendModel(0, 0, 0, 0, 0.8, null, 0, 0);

        var suggestions = WeeklyCheckInCalculator.GenerateSuggestions(summary, trends, null);

        Assert.Contains("suggestion.weight_increasing", suggestions);
    }

    [Fact]
    public void GenerateSuggestions_WhenLowHydration_SuggestsDrinkMore() {
        var summary = new WeekSummaryModel(14000, 2000, 120, 70, 230, 21, 7, null, null, null, null, 7000, 1000);
        var trends = new WeekTrendModel(0, 0, 0, 0, null, null, 0, 0);

        var suggestions = WeeklyCheckInCalculator.GenerateSuggestions(summary, trends, null);

        Assert.Contains("suggestion.drink_more_water", suggestions);
    }

    [Fact]
    public void GenerateSuggestions_WhenLowProtein_SuggestsLowProtein() {
        var summary = new WeekSummaryModel(14000, 2000, 40, 70, 230, 21, 7, null, null, null, null, 0, 0);
        var trends = new WeekTrendModel(0, 0, 0, 0, null, null, 0, 0);

        var suggestions = WeeklyCheckInCalculator.GenerateSuggestions(summary, trends, null);

        Assert.Contains("suggestion.low_protein", suggestions);
    }

    [Fact]
    public void GenerateSuggestions_WhenNoIssues_SuggestsKeepGoing() {
        var summary = new WeekSummaryModel(14000, 2000, 120, 70, 230, 21, 7, null, null, null, null, 14000, 2000);
        var trends = new WeekTrendModel(0, 0, 0, 0, 0, 0, 0, 0);

        var suggestions = WeeklyCheckInCalculator.GenerateSuggestions(summary, trends, null);

        Assert.Contains("suggestion.keep_going", suggestions);
    }
}
