using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Tests.Domain.ValueObjects;

public class ValueObjectsInvariantTests {
    [Fact]
    public void DailySymptoms_Create_WithOutOfRangeValue_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            DailySymptoms.Create(-1, 0, 0, 0, 0, 0, 0));
    }

    [Fact]
    public void DailySymptoms_Create_AndUpdate_PreserveValueEquality() {
        var source = DailySymptoms.Create(1, 2, 3, 4, 5, 6, 7);
        var same = DailySymptoms.Create(1, 2, 3, 4, 5, 6, 7);
        var updated = source.Update(mood: 8);

        Assert.Equal(source, same);
        Assert.Equal(source.GetHashCode(), same.GetHashCode());
        Assert.NotEqual(source, updated);
        Assert.Equal(8, updated.Mood);
        Assert.Equal(1, updated.Pain);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void DesiredWeight_Create_WithNonFiniteValue_Throws(double value) {
        Assert.Throws<ArgumentOutOfRangeException>(() => DesiredWeight.Create(value));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void DesiredWaist_Create_WithNonFiniteValue_Throws(double value) {
        Assert.Throws<ArgumentOutOfRangeException>(() => DesiredWaist.Create(value));
    }

    [Fact]
    public void ProductNutrition_Create_WithNegativeValue_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ProductNutrition.Create(-1, 0, 0, 0, 0, 0));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void ProductNutrition_Create_WithNonFiniteValue_Throws(double value) {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ProductNutrition.Create(value, 0, 0, 0, 0, 0));
    }

    [Fact]
    public void ProductNutrition_IsCloseTo_RespectsEpsilon() {
        var left = ProductNutrition.Create(100, 10, 5, 20, 3, 0);
        var right = ProductNutrition.Create(100.0000005, 10, 5, 20, 3, 0);
        var far = ProductNutrition.Create(100.1, 10, 5, 20, 3, 0);

        Assert.True(left.IsCloseTo(right, 0.000001));
        Assert.False(left.IsCloseTo(far, 0.000001));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void RecipeNutrition_Create_WithNonFiniteValue_Throws(double value) {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            RecipeNutrition.Create(value, null, null, null, null, null));
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
            UserNutritionGoals.Create(value, null, null, null, null, null));
    }

    [Fact]
    public void UserNutritionGoals_With_UpdatesOnlyProvidedValues() {
        var original = UserNutritionGoals.Create(2000, 120, 70, 230, 30, 2.5);
        var updated = original.With(proteinTarget: 130);

        Assert.Equal(2000, updated.DailyCalorieTarget);
        Assert.Equal(130, updated.ProteinTarget);
        Assert.Equal(70, updated.FatTarget);
        Assert.Equal(230, updated.CarbTarget);
        Assert.Equal(30, updated.FiberTarget);
        Assert.Equal(2.5, updated.WaterGoal);
    }

    [Fact]
    public void GenderCode_TryParse_NormalizesAndValidates() {
        var ok = GenderCode.TryParse("  f ", out var gender);
        var invalid = GenderCode.TryParse("x", out _);

        Assert.True(ok);
        Assert.Equal("F", gender.Value);
        Assert.False(invalid);
    }

    [Fact]
    public void LanguageCode_TryParse_AndFromPreferred_WorkAsExpected() {
        var parsed = LanguageCode.TryParse("  EN  ", out var en);
        var preferredRu = LanguageCode.FromPreferred("ru-RU");
        var preferredDefault = LanguageCode.FromPreferred("de-DE");

        Assert.True(parsed);
        Assert.Equal("en", en.Value);
        Assert.Equal("ru", preferredRu.Value);
        Assert.Equal("en", preferredDefault.Value);
    }

    [Fact]
    public void EmailAddress_Create_NormalizesValue() {
        var email = EmailAddress.Create("  USER@Example.COM ");

        Assert.Equal("user@example.com", email.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("invalid")]
    [InlineData("user@")]
    public void EmailAddress_Create_WithInvalidValue_Throws(string value) {
        Assert.Throws<ArgumentException>(() => EmailAddress.Create(value));
    }
}
