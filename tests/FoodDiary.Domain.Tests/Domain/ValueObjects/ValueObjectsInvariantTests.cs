using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Domain.Tests.Domain.ValueObjects;

[ExcludeFromCodeCoverage]
public class ValueObjectsInvariantTests {

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void RecipeNutrition_Create_WithNonFiniteValue_Throws(double value) {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            RecipeNutrition.Create(value, proteins: null, fats: null, carbs: null, fiber: null, alcohol: null));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void UserActivityGoals_Create_WithNonFiniteHydration_Throws(double value) {
        Assert.Throws<ArgumentOutOfRangeException>(() => UserActivityGoals.Create(10000, value));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void UserNutritionGoals_Create_WithNonFiniteValue_Throws(double value) {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            UserNutritionGoals.Create(value, proteinTarget: null, fatTarget: null, carbTarget: null, fiberTarget: null, waterGoal: null));
    }

    [Theory]
    [InlineData(-1d)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void UserCalorieSchedule_WithInvalidDailyTarget_Throws(double value) {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CreateCalorieSchedule(dailyCalorieTarget: value));
    }

    [Theory]
    [InlineData(-1d)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void UserCalorieSchedule_WithInvalidDayTarget_Throws(double value) {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CreateCalorieSchedule(dailyCalorieTarget: 2000, calorieCyclingEnabled: true, mondayCalories: value));
    }

    [Fact]
    public void UserCalorieSchedule_WhenDailyTargetOverflowsWeeklyTotal_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CreateCalorieSchedule(dailyCalorieTarget: double.MaxValue));
    }

    [Fact]
    public void UserCalorieSchedule_WhenDayTargetsOverflowWeeklyTotal_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CreateCalorieSchedule(
                dailyCalorieTarget: 0,
                calorieCyclingEnabled: true,
                mondayCalories: double.MaxValue,
                tuesdayCalories: double.MaxValue));
    }

    [Fact]
    public void UserNutritionGoals_With_UpdatesOnlyProvidedValues() {
        var original = UserNutritionGoals.Create(2000, 120, 70, 230, 30, 2.5);
        UserNutritionGoals updated = original.With(proteinTarget: 130);

        Assert.Multiple(
            () => Assert.Equal(2000, updated.DailyCalorieTarget),
            () => Assert.Equal(130, updated.ProteinTarget),
            () => Assert.Equal(70, updated.FatTarget),
            () => Assert.Equal(230, updated.CarbTarget),
            () => Assert.Equal(30, updated.FiberTarget),
            () => Assert.Equal(2.5, updated.WaterGoal));
    }

    [Fact]
    public void GenderCode_TryParse_NormalizesAndValidates() {
        bool ok = GenderCode.TryParse("  f ", out GenderCode gender);
        bool invalid = GenderCode.TryParse("x", out _);

        Assert.Multiple(
            () => Assert.True(ok),
            () => Assert.Equal("F", gender.Value),
            () => Assert.False(invalid));
    }

    private static UserCalorieSchedule CreateCalorieSchedule(
        double? dailyCalorieTarget = null,
        bool calorieCyclingEnabled = false,
        double? mondayCalories = null,
        double? tuesdayCalories = null) {
        return new UserCalorieSchedule(
            DailyCalorieTarget: dailyCalorieTarget,
            CalorieCyclingEnabled: calorieCyclingEnabled,
            MondayCalories: mondayCalories,
            TuesdayCalories: tuesdayCalories,
            WednesdayCalories: null,
            ThursdayCalories: null,
            FridayCalories: null,
            SaturdayCalories: null,
            SundayCalories: null);
    }
}
