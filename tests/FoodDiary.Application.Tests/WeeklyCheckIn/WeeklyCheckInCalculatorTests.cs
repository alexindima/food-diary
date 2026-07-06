using FoodDiary.Application.Abstractions.Dashboard.Models;
using FoodDiary.Application.WaistEntries.Models;
using FoodDiary.Application.WeeklyCheckIn.Models;
using FoodDiary.Application.WeeklyCheckIn.Services;
using FoodDiary.Application.WeightEntries.Models;

namespace FoodDiary.Application.Tests.WeeklyCheckIn;

[ExcludeFromCodeCoverage]
public class WeeklyCheckInCalculatorTests {
    private static readonly Guid TestUserId = Guid.NewGuid();
    private static readonly DateTime WeekStart = new(2026, 3, 30, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void BuildSummary_WithEmptyData_ReturnsZeros() {
        WeekSummaryModel summary = WeeklyCheckInCalculator.BuildSummary(
            [], mealsLogged: 0, [], [], [], daysInPeriod: 7);

        Assert.Equal(0, summary.TotalCalories);
        Assert.Equal(0, summary.AvgDailyCalories);
        Assert.Equal(0, summary.MealsLogged);
        Assert.Equal(0, summary.DaysLogged);
        Assert.Null(summary.WeightStart);
        Assert.Null(summary.WaistStart);
    }

    [Fact]
    public void BuildSummary_WithMeals_CalculatesAveragesCorrectly() {
        IReadOnlyList<DashboardStatisticsBucketReadModel> buckets = [
            CreateNutritionBucket(WeekStart, 700, 40, 25, 80, 8),
            CreateNutritionBucket(WeekStart.AddDays(1), 700, 40, 25, 80, 8),
            CreateNutritionBucket(WeekStart.AddDays(2), 700, 40, 25, 80, 8),
        ];

        WeekSummaryModel summary = WeeklyCheckInCalculator.BuildSummary(
            buckets, mealsLogged: 3, [], [], [], daysInPeriod: 7);

        Assert.Equal(2100, summary.TotalCalories);
        Assert.Equal(300, summary.AvgDailyCalories);
        Assert.Equal(3, summary.MealsLogged);
        Assert.Equal(3, summary.DaysLogged);
    }

    [Fact]
    public void BuildSummary_WithWeightsAndWaists_TracksStartAndEnd() {
        var weights = new List<WeightEntryModel> {
            CreateWeightEntry(WeekStart, 80),
            CreateWeightEntry(WeekStart.AddDays(6), 79.5),
        };
        var waists = new List<WaistEntryModel> {
            CreateWaistEntry(WeekStart, 85),
            CreateWaistEntry(WeekStart.AddDays(6), 84),
        };

        WeekSummaryModel summary = WeeklyCheckInCalculator.BuildSummary(
            [], mealsLogged: 0, weights, waists, [], daysInPeriod: 7);

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

        WeekSummaryModel summary = WeeklyCheckInCalculator.BuildSummary(
            [], mealsLogged: 0, [], [], hydration, daysInPeriod: 7);

        Assert.Equal(3800, summary.TotalHydrationMl);
        Assert.Equal(542, summary.AvgDailyHydrationMl);
    }

    [Fact]
    public void BuildTrends_CalculatesDifferences() {
        var thisWeek = new WeekSummaryModel(14000, 2000, 120, 70, 230, 21, 7, WeightStart: null, 79.5, WaistStart: null, 84, 14000, 2000);
        var lastWeek = new WeekSummaryModel(12000, 1714.3, 100, 65, 200, 18, 6, WeightStart: null, 80, WaistStart: null, 85, 12000, 1714);

        WeekTrendModel trends = WeeklyCheckInCalculator.BuildTrends(thisWeek, lastWeek);

        Assert.Equal(285.7, trends.CalorieChange);
        Assert.Equal(20, trends.ProteinChange);
        Assert.Equal(-0.5, trends.WeightChange);
        Assert.Equal(-1, trends.WaistChange);
        Assert.Equal(3, trends.MealsLoggedChange);
    }

    [Fact]
    public void BuildTrends_WithNullWeights_ReturnsNullChanges() {
        var thisWeek = new WeekSummaryModel(14000, 2000, 120, 70, 230, 21, 7, WeightStart: null, WeightEnd: null, WaistStart: null, WaistEnd: null, 0, 0);
        var lastWeek = new WeekSummaryModel(12000, 1714, 100, 65, 200, 18, 6, WeightStart: null, 80, WaistStart: null, 85, 0, 0);

        WeekTrendModel trends = WeeklyCheckInCalculator.BuildTrends(thisWeek, lastWeek);

        Assert.Null(trends.WeightChange);
        Assert.Null(trends.WaistChange);
    }

    [Fact]
    public void GenerateSuggestions_WhenFewDaysLogged_SuggestsLogMore() {
        var summary = new WeekSummaryModel(6000, 1500, 80, 50, 180, 6, 3, WeightStart: null, WeightEnd: null, WaistStart: null, WaistEnd: null, 0, 0);
        var trends = new WeekTrendModel(0, 0, 0, 0, WeightChange: null, WaistChange: null, 0, 0);

        IReadOnlyList<string> suggestions = WeeklyCheckInCalculator.GenerateSuggestions(summary, trends, 2000);

        Assert.Contains("suggestion.log_more_days", suggestions);
    }

    [Fact]
    public void GenerateSuggestions_WhenOverCalorieGoal_SuggestsOver() {
        var summary = new WeekSummaryModel(17500, 2500, 130, 90, 280, 21, 7, WeightStart: null, WeightEnd: null, WaistStart: null, WaistEnd: null, 0, 0);
        var trends = new WeekTrendModel(0, 0, 0, 0, WeightChange: null, WaistChange: null, 0, 0);

        IReadOnlyList<string> suggestions = WeeklyCheckInCalculator.GenerateSuggestions(summary, trends, 2000);

        Assert.Contains("suggestion.over_calorie_goal", suggestions);
    }

    [Fact]
    public void GenerateSuggestions_WhenOnTrack_SuggestsOnTrack() {
        var summary = new WeekSummaryModel(14000, 2000, 120, 70, 230, 21, 7, WeightStart: null, WeightEnd: null, WaistStart: null, WaistEnd: null, 14000, 2000);
        var trends = new WeekTrendModel(0, 0, 0, 0, 0, 0, 0, 0);

        IReadOnlyList<string> suggestions = WeeklyCheckInCalculator.GenerateSuggestions(summary, trends, 2000);

        Assert.Contains("suggestion.on_track", suggestions);
    }

    [Fact]
    public void GenerateSuggestions_WhenWeightIncreasing_SuggestsWeightIncreasing() {
        var summary = new WeekSummaryModel(14000, 2000, 120, 70, 230, 21, 7, WeightStart: null, WeightEnd: null, WaistStart: null, WaistEnd: null, 0, 0);
        var trends = new WeekTrendModel(0, 0, 0, 0, 0.8, WaistChange: null, 0, 0);

        IReadOnlyList<string> suggestions = WeeklyCheckInCalculator.GenerateSuggestions(summary, trends, dailyCalorieTarget: null);

        Assert.Contains("suggestion.weight_increasing", suggestions);
    }

    [Fact]
    public void GenerateSuggestions_WhenWeightDecreasing_SuggestsWeightDecreasing() {
        var summary = new WeekSummaryModel(14000, 2000, 120, 70, 230, 21, 7, WeightStart: null, WeightEnd: null, WaistStart: null, WaistEnd: null, 0, 0);
        var trends = new WeekTrendModel(0, 0, 0, 0, -0.8, WaistChange: null, 0, 0);

        IReadOnlyList<string> suggestions = WeeklyCheckInCalculator.GenerateSuggestions(summary, trends, dailyCalorieTarget: null);

        Assert.Contains("suggestion.weight_decreasing", suggestions);
    }

    [Fact]
    public void GenerateSuggestions_WhenLowHydration_SuggestsDrinkMore() {
        var summary = new WeekSummaryModel(14000, 2000, 120, 70, 230, 21, 7, WeightStart: null, WeightEnd: null, WaistStart: null, WaistEnd: null, 7000, 1000);
        var trends = new WeekTrendModel(0, 0, 0, 0, WeightChange: null, WaistChange: null, 0, 0);

        IReadOnlyList<string> suggestions = WeeklyCheckInCalculator.GenerateSuggestions(summary, trends, dailyCalorieTarget: null);

        Assert.Contains("suggestion.drink_more_water", suggestions);
    }

    [Fact]
    public void GenerateSuggestions_WhenLowProtein_SuggestsLowProtein() {
        var summary = new WeekSummaryModel(14000, 2000, 40, 70, 230, 21, 7, WeightStart: null, WeightEnd: null, WaistStart: null, WaistEnd: null, 0, 0);
        var trends = new WeekTrendModel(0, 0, 0, 0, WeightChange: null, WaistChange: null, 0, 0);

        IReadOnlyList<string> suggestions = WeeklyCheckInCalculator.GenerateSuggestions(summary, trends, dailyCalorieTarget: null);

        Assert.Contains("suggestion.low_protein", suggestions);
    }

    [Fact]
    public void GenerateSuggestions_WhenNoIssues_SuggestsKeepGoing() {
        var summary = new WeekSummaryModel(14000, 2000, 120, 70, 230, 21, 7, WeightStart: null, WeightEnd: null, WaistStart: null, WaistEnd: null, 14000, 2000);
        var trends = new WeekTrendModel(0, 0, 0, 0, 0, 0, 0, 0);

        IReadOnlyList<string> suggestions = WeeklyCheckInCalculator.GenerateSuggestions(summary, trends, dailyCalorieTarget: null);

        Assert.Contains("suggestion.keep_going", suggestions);
    }

    private static DashboardStatisticsBucketReadModel CreateNutritionBucket(
        DateTime date,
        double calories,
        double proteins,
        double fats,
        double carbs,
        double fiber) =>
        new(date, date, calories, proteins, fats, carbs, fiber, proteins, fats, carbs, fiber);

    private static WeightEntryModel CreateWeightEntry(DateTime date, double weight) =>
        new(Guid.NewGuid(), TestUserId, date, weight);

    private static WaistEntryModel CreateWaistEntry(DateTime date, double circumference) =>
        new(Guid.NewGuid(), TestUserId, date, circumference);
}
