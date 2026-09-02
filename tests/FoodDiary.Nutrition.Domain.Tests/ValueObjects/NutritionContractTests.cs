using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Nutrition.Domain.Tests.ValueObjects;

[ExcludeFromCodeCoverage]
public sealed class NutritionContractTests {
    [Fact]
    public void Enums_PreserveNamesAndNumericValues() {
        Assert.Equal(["G", "Ml", "Pcs"], Enum.GetNames<MeasurementUnit>(), StringComparer.Ordinal);
        Assert.Equal([0, 1, 2], Enum.GetValues<MeasurementUnit>().Select(value => (int)value));
        Assert.Equal(["Red", "Yellow", "Green"], Enum.GetNames<FoodQualityGrade>(), StringComparer.Ordinal);
        Assert.Equal([0, 1, 2], Enum.GetValues<FoodQualityGrade>().Select(value => (int)value));
        Assert.Equal(["Unknown", "Meat", "Fruit", "Vegetable", "Cheese", "Dairy", "Seafood", "Grain", "Beverage", "Dessert", "Other"], Enum.GetNames<ProductType>(), StringComparer.Ordinal);
        Assert.Equal(Enumerable.Range(0, 11), Enum.GetValues<ProductType>().Select(value => (int)value));
    }

    [Theory]
    [InlineData(0, FoodQualityGrade.Red)]
    [InlineData(33, FoodQualityGrade.Red)]
    [InlineData(34, FoodQualityGrade.Yellow)]
    [InlineData(66, FoodQualityGrade.Yellow)]
    [InlineData(67, FoodQualityGrade.Green)]
    [InlineData(100, FoodQualityGrade.Green)]
    public void Constructor_PreservesGradeBoundaries(int score, FoodQualityGrade grade) {
        var quality = new FoodQualityScore(score, grade);
        Assert.Multiple(() => Assert.Equal(score, quality.Score), () => Assert.Equal(grade, quality.Grade));
        ArgumentException error = Assert.Throws<ArgumentException>(() => new FoodQualityScore(score, (FoodQualityGrade)99));
        Assert.Equal("grade", error.ParamName);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Constructor_RejectsOutOfRangeScore(int score) {
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new FoodQualityScore(score, FoodQualityGrade.Red));
        Assert.Equal("score", error.ParamName);
    }

    [Theory]
    [InlineData(0, "caloriesPerBase")]
    [InlineData(1, "proteinsPerBase")]
    [InlineData(2, "fatsPerBase")]
    [InlineData(3, "carbsPerBase")]
    [InlineData(4, "fiberPerBase")]
    [InlineData(5, "alcoholPerBase")]
    public void Calculate_ValidatesEveryNutrientBeforeZeroCalorieFallback(int index, string parameterName) {
        foreach (double invalid in new[] { -1d, double.NaN, double.PositiveInfinity, double.NegativeInfinity }) {
            double[] values = [0, 0, 0, 0, 0, 0];
            values[index] = invalid;
            ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() =>
                FoodQualityScore.Calculate(values[0], values[1], values[2], values[3], values[4], values[5]));
            Assert.Equal(parameterName, error.ParamName);
            string reason = double.IsFinite(invalid) ? "Value must be non-negative." : "Value must be a finite number.";
            Assert.StartsWith(reason, error.Message, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Calculate_RejectsUndefinedClassificationBeforeFallback() {
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() =>
            FoodQualityScore.Calculate(0, 0, 0, 0, 0, 0, (ProductType)99));
        Assert.Equal("productType", error.ParamName);
        Assert.StartsWith("Value must be one of the supported values.", error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(ProductType.Unknown, 25)]
    [InlineData(ProductType.Meat, 30)]
    [InlineData(ProductType.Fruit, 35)]
    [InlineData(ProductType.Vegetable, 40)]
    [InlineData(ProductType.Cheese, 23)]
    [InlineData(ProductType.Dairy, 28)]
    [InlineData(ProductType.Seafood, 33)]
    [InlineData(ProductType.Grain, 27)]
    [InlineData(ProductType.Beverage, 20)]
    [InlineData(ProductType.Dessert, 15)]
    [InlineData(ProductType.Other, 25)]
    public void Calculate_PreservesClassificationModifiers(ProductType type, int expected) {
        Assert.Equal(expected, FoodQualityScore.Calculate(100, 0, 0, 0, 0, 0, type).Score);
    }

    [Theory]
    [InlineData(0.1, 26)]
    [InlineData(0.3, 26)]
    public void Calculate_RoundsMidpointsToEven(double fiber, int expected) {
        Assert.Equal(expected, FoodQualityScore.Calculate(100, 0, 0, 0, fiber, 0).Score);
    }

    [Fact]
    public void Calculate_ClampsBothEnds() {
        Assert.Equal(new FoodQualityScore(0, FoodQualityGrade.Red), FoodQualityScore.Calculate(600, 0, 0, 10, 0, 50, ProductType.Dessert));
        Assert.Equal(new FoodQualityScore(100, FoodQualityGrade.Green), FoodQualityScore.Calculate(10, 50, 0, 0, 50, 0, ProductType.Vegetable));
    }
}
