using FoodDiary.Application.Tdee.Models;
using FoodDiary.Application.Tdee.Services;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Tdee;

[ExcludeFromCodeCoverage]
public class TdeeCalculatorTests {
    private static readonly UserId TestUserId = UserId.New();

    [Fact]
    public void CalculateAdaptive_WithTooFewWeightEntries_ReturnsInsufficient() {
        IReadOnlyList<WeightEntry> weights = CreateWeightEntries(2, startDate: DateTime.UtcNow.AddDays(-30), startWeight: 80);
        IReadOnlyList<Meal> meals = CreateMeals(20, startDate: DateTime.UtcNow.AddDays(-30), caloriesPerMeal: 500);

        AdaptiveTdeeResult result = TdeeCalculator.CalculateAdaptive(weights, meals, 30);

        Assert.False(result.HasData);
        Assert.Equal(TdeeConfidence.None, result.Confidence);
    }

    [Fact]
    public void CalculateAdaptive_WithTooFewDays_ReturnsInsufficient() {
        IReadOnlyList<WeightEntry> weights = CreateWeightEntries(5, startDate: DateTime.UtcNow.AddDays(-10), startWeight: 80);
        IReadOnlyList<Meal> meals = CreateMeals(10, startDate: DateTime.UtcNow.AddDays(-10), caloriesPerMeal: 500);

        AdaptiveTdeeResult result = TdeeCalculator.CalculateAdaptive(weights, meals, 10);

        Assert.False(result.HasData);
    }

    [Fact]
    public void CalculateAdaptive_WithTooFewCalorieDays_ReturnsInsufficient() {
        DateTime baseDate = DateTime.UtcNow.AddDays(-30);
        IReadOnlyList<WeightEntry> weights = CreateWeightEntries(5, startDate: baseDate, startWeight: 80);
        IReadOnlyList<Meal> meals = CreateMeals(13, startDate: baseDate, caloriesPerMeal: 2000);

        AdaptiveTdeeResult result = TdeeCalculator.CalculateAdaptive(weights, meals, 30);

        Assert.False(result.HasData);
    }

    [Fact]
    public void CalculateAdaptive_WithCompressedWeightDates_ReturnsInsufficient() {
        DateTime baseDate = DateTime.UtcNow.AddDays(-10);
        var weights = Enumerable.Range(0, 5)
            .Select(index => WeightEntry.Create(TestUserId, baseDate.AddDays(index), 80))
            .ToList();
        IReadOnlyList<Meal> meals = CreateMeals(20, startDate: baseDate.AddDays(-10), caloriesPerMeal: 2000);

        AdaptiveTdeeResult result = TdeeCalculator.CalculateAdaptive(weights, meals, 30);

        Assert.False(result.HasData);
    }

    [Fact]
    public void CalculateAdaptive_WithUnreasonableTdee_ReturnsInsufficient() {
        DateTime baseDate = DateTime.UtcNow.AddDays(-30);
        IReadOnlyList<WeightEntry> weights = CreateWeightEntries(10, startDate: baseDate, startWeight: 80, weightChangePerEntry: 1);
        IReadOnlyList<Meal> meals = CreateMeals(30, startDate: baseDate, caloriesPerMeal: 900);

        AdaptiveTdeeResult result = TdeeCalculator.CalculateAdaptive(weights, meals, 30);

        Assert.False(result.HasData);
    }

    [Fact]
    public void CalculateAdaptive_WithSufficientData_MaintainingWeight_ReturnsTdeeCloseToIntake() {
        DateTime baseDate = DateTime.UtcNow.AddDays(-30);
        // Stable weight at 80 kg over 30 days
        IReadOnlyList<WeightEntry> weights = CreateWeightEntries(10, startDate: baseDate, startWeight: 80, weightChangePerEntry: 0);
        // Eating ~2000 kcal/day
        IReadOnlyList<Meal> meals = CreateMeals(30, startDate: baseDate, caloriesPerMeal: 2000);

        AdaptiveTdeeResult result = TdeeCalculator.CalculateAdaptive(weights, meals, 30);

        Assert.True(result.HasData);
        // TDEE should be close to intake when weight is stable
        Assert.InRange(result.AdaptiveTdee!.Value, 1800, 2200);
    }

    [Fact]
    public void CalculateAdaptive_WithWeightLoss_ReturnsTdeeHigherThanIntake() {
        DateTime baseDate = DateTime.UtcNow.AddDays(-30);
        // Losing ~1 kg over 30 days (80 -> 79)
        IReadOnlyList<WeightEntry> weights = CreateWeightEntries(10, startDate: baseDate, startWeight: 80, weightChangePerEntry: -0.1);
        // Eating 1800 kcal/day
        IReadOnlyList<Meal> meals = CreateMeals(30, startDate: baseDate, caloriesPerMeal: 1800);

        AdaptiveTdeeResult result = TdeeCalculator.CalculateAdaptive(weights, meals, 30);

        Assert.True(result.HasData);
        Assert.True(result.AdaptiveTdee > 1800);
    }

    [Fact]
    public void CalculateAdaptive_HighConfidence_WithExtensiveData() {
        DateTime baseDate = DateTime.UtcNow.AddDays(-35);
        IReadOnlyList<WeightEntry> weights = CreateWeightEntries(12, startDate: baseDate, startWeight: 80, weightChangePerEntry: 0);
        IReadOnlyList<Meal> meals = CreateMeals(30, startDate: baseDate, caloriesPerMeal: 2000);

        AdaptiveTdeeResult result = TdeeCalculator.CalculateAdaptive(weights, meals, 35);

        Assert.True(result.HasData);
        Assert.Equal(TdeeConfidence.High, result.Confidence);
    }

    [Fact]
    public void CalculateAdaptive_MediumConfidence_WithModerateData() {
        DateTime baseDate = DateTime.UtcNow.AddDays(-24);
        IReadOnlyList<WeightEntry> weights = CreateWeightEntries(5, startDate: baseDate, startWeight: 80, weightChangePerEntry: 0);
        IReadOnlyList<Meal> meals = CreateMeals(20, startDate: baseDate, caloriesPerMeal: 2000);

        AdaptiveTdeeResult result = TdeeCalculator.CalculateAdaptive(weights, meals, 24);

        Assert.True(result.HasData);
        Assert.Equal(TdeeConfidence.Medium, result.Confidence);
    }

    [Fact]
    public void CalculateAdaptive_LowConfidence_WithMinimumData() {
        DateTime baseDate = DateTime.UtcNow.AddDays(-16);
        IReadOnlyList<WeightEntry> weights = CreateWeightEntries(3, startDate: baseDate, startWeight: 80, weightChangePerEntry: 0);
        IReadOnlyList<Meal> meals = CreateMeals(14, startDate: baseDate, caloriesPerMeal: 2000);

        AdaptiveTdeeResult result = TdeeCalculator.CalculateAdaptive(weights, meals, 16);

        Assert.True(result.HasData);
        Assert.Equal(TdeeConfidence.Low, result.Confidence);
    }

    [Fact]
    public void SuggestCalorieTarget_WithNoWeightGoal_ReturnsTdeeRounded() {
        double? target = TdeeCalculator.SuggestCalorieTarget(2200, currentWeight: null, desiredWeight: null);

        Assert.Equal(2200, target);
    }

    [Fact]
    public void SuggestCalorieTarget_WhenLosingWeight_Returns500Deficit() {
        double? target = TdeeCalculator.SuggestCalorieTarget(2200, currentWeight: 90, desiredWeight: 80);

        Assert.Equal(1700, target);
    }

    [Fact]
    public void SuggestCalorieTarget_WhenGainingWeight_Returns300Surplus() {
        double? target = TdeeCalculator.SuggestCalorieTarget(2200, currentWeight: 60, desiredWeight: 70);

        Assert.Equal(2500, target);
    }

    [Fact]
    public void SuggestCalorieTarget_WhenAtGoalWeight_ReturnsMaintenance() {
        double? target = TdeeCalculator.SuggestCalorieTarget(2200, currentWeight: 80, desiredWeight: 80);

        Assert.Equal(2200, target);
    }

    [Fact]
    public void SuggestCalorieTarget_NeverGoesBelowMinimum() {
        double? target = TdeeCalculator.SuggestCalorieTarget(1300, currentWeight: 90, desiredWeight: 70);

        Assert.Equal(1200, target);
    }

    [Fact]
    public void GetGoalAdjustmentHint_WithNullTdee_ReturnsNull() {
        Assert.Null(TdeeCalculator.GetGoalAdjustmentHint(adaptiveTdee: null, 2000, 80, 70));
    }

    [Fact]
    public void GetGoalAdjustmentHint_WithNullTarget_ReturnsNull() {
        Assert.Null(TdeeCalculator.GetGoalAdjustmentHint(2000, currentTarget: null, 80, 70));
    }

    [Fact]
    public void GetGoalAdjustmentHint_WhenDeficitTooAggressive_ReturnsCorrectHint() {
        // Target is 1200, TDEE is 2200 => diff = -1000, losing weight
        string? hint = TdeeCalculator.GetGoalAdjustmentHint(2200, 1200, 90, 80);

        Assert.Equal("hint.deficit_too_aggressive", hint);
    }

    [Theory]
    [InlineData(2200, 1800, 90, 80, "hint.deficit_moderate")]
    [InlineData(2200, 2000, 90, 80, "hint.deficit_mild")]
    [InlineData(2200, 2300, 90, 80, "hint.surplus_while_losing_goal")]
    [InlineData(2200, 2800, 70, 80, "hint.surplus_too_aggressive")]
    [InlineData(2200, 2500, 70, 80, "hint.surplus_moderate")]
    [InlineData(2200, 1500, 70, 80, "hint.review_goals")]
    public void GetGoalAdjustmentHint_ReturnsExpectedHintForGoalState(
        double adaptiveTdee,
        double currentTarget,
        double currentWeight,
        double desiredWeight,
        string expected) {
        string? hint = TdeeCalculator.GetGoalAdjustmentHint(adaptiveTdee, currentTarget, currentWeight, desiredWeight);

        Assert.Equal(expected, hint);
    }

    [Fact]
    public void GetGoalAdjustmentHint_WhenMaintenanceOnTrack_ReturnsCorrectHint() {
        // Target = TDEE, not losing or gaining
        string? hint = TdeeCalculator.GetGoalAdjustmentHint(2200, 2200, 80, 80);

        Assert.Equal("hint.maintenance_on_track", hint);
    }

    [Fact]
    public void CalculateAdaptive_WithExercise_IncreasesTdee() {
        DateTime baseDate = DateTime.UtcNow.AddDays(-30);
        IReadOnlyList<WeightEntry> weights = CreateWeightEntries(10, startDate: baseDate, startWeight: 80, weightChangePerEntry: 0);
        IReadOnlyList<Meal> meals = CreateMeals(30, startDate: baseDate, caloriesPerMeal: 2000);

        AdaptiveTdeeResult resultWithout = TdeeCalculator.CalculateAdaptive(weights, meals, 30);
        IReadOnlyList<ExerciseEntry> exercises = CreateExerciseEntries(30, startDate: baseDate, caloriesPerDay: 300);
        AdaptiveTdeeResult resultWith = TdeeCalculator.CalculateAdaptive(weights, meals, 30, exercises);

        Assert.True(resultWithout.HasData);
        Assert.True(resultWith.HasData);
        Assert.True(resultWith.AdaptiveTdee > resultWithout.AdaptiveTdee);
    }

    [Fact]
    public void CalculateAdaptive_EmaSmoothing_HandlesNoisyWeights() {
        DateTime baseDate = DateTime.UtcNow.AddDays(-30);
        // Noisy weights around 80 kg - EMA should smooth them
        var weights = new List<WeightEntry>();
        double[] noisy = [80.5, 79.2, 80.8, 79.5, 80.3, 79.8, 80.1, 79.9, 80.0, 80.2];
        double daysPerEntry = 30.0 / (noisy.Length - 1);
        for (int i = 0; i < noisy.Length; i++) {
            weights.Add(WeightEntry.Create(TestUserId, baseDate.AddDays(i * daysPerEntry), noisy[i]));
        }

        IReadOnlyList<Meal> meals = CreateMeals(30, startDate: baseDate, caloriesPerMeal: 2000);

        AdaptiveTdeeResult result = TdeeCalculator.CalculateAdaptive(weights, meals, 30);

        Assert.True(result.HasData);
        // With noisy but stable weight, TDEE should be close to intake
        Assert.InRange(result.AdaptiveTdee!.Value, 1800, 2200);
    }

    private static IReadOnlyList<ExerciseEntry> CreateExerciseEntries(
        int count, DateTime startDate, double caloriesPerDay) {
        var entries = new List<ExerciseEntry>();
        for (int i = 0; i < count; i++) {
            DateTime date = startDate.AddDays(i);
            entries.Add(ExerciseEntry.Create(TestUserId, date, ExerciseType.Running, 30, caloriesPerDay));
        }

        return entries;
    }

    private static IReadOnlyList<WeightEntry> CreateWeightEntries(
        int count, DateTime startDate, double startWeight, double weightChangePerEntry = 0) {
        var entries = new List<WeightEntry>();
        double daysPerEntry = count > 1 ? 30.0 / (count - 1) : 1;
        for (int i = 0; i < count; i++) {
            DateTime date = startDate.AddDays(i * daysPerEntry);
            entries.Add(WeightEntry.Create(TestUserId, date, startWeight + (i * weightChangePerEntry)));
        }

        return entries;
    }

    private static IReadOnlyList<Meal> CreateMeals(int count, DateTime startDate, double caloriesPerMeal) {
        var meals = new List<Meal>();
        for (int i = 0; i < count; i++) {
            DateTime date = startDate.AddDays(i);
            var meal = Meal.Create(TestUserId, date, MealType.Lunch);
            meal.ApplyNutrition(new MealNutritionUpdate(
                caloriesPerMeal, caloriesPerMeal * 0.15, caloriesPerMeal * 0.035,
                caloriesPerMeal * 0.05, 5, 0, IsAutoCalculated: true));
            meals.Add(meal);
        }

        return meals;
    }
}
