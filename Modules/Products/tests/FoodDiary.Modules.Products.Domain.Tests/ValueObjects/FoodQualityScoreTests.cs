using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Domain.Tests.ValueObjects;

[ExcludeFromCodeCoverage]
public class FoodQualityScoreTests {
    // --- FoodQualityScore ---

    [Fact]
    public void FoodQualityScore_Calculate_WithZeroCalories_ReturnsYellow50() {
        var result = FoodQualityScore.Calculate(0, 0, 0, 0, 0, 0);

        Assert.Equal(50, result.Score);
        Assert.Equal(FoodQualityGrade.Yellow, result.Grade);
    }

    [Fact]
    public void FoodQualityScore_Calculate_HighProteinLowCalDensity_ReturnsHighScore() {
        // High protein, high fiber, low calorie density = healthy food
        var result = FoodQualityScore.Calculate(
            caloriesPerBase: 50, proteinsPerBase: 10, fatsPerBase: 1,
            carbsPerBase: 5, fiberPerBase: 5, alcoholPerBase: 0,
            productType: ProductType.Vegetable);

        Assert.True(result.Score >= 67);
        Assert.Equal(FoodQualityGrade.Green, result.Grade);
    }

    [Fact]
    public void FoodQualityScore_Calculate_HighCalDensityWithAlcohol_ReturnsLowScore() {
        var result = FoodQualityScore.Calculate(
            caloriesPerBase: 500, proteinsPerBase: 0, fatsPerBase: 0,
            carbsPerBase: 10, fiberPerBase: 0, alcoholPerBase: 50,
            productType: ProductType.Beverage);

        Assert.True(result.Score < 34);
        Assert.Equal(FoodQualityGrade.Red, result.Grade);
    }

    [Fact]
    public void FoodQualityScore_Calculate_VegetableModifier_IncreasesScore() {
        var withoutType = FoodQualityScore.Calculate(100, 5, 2, 10, 3, 0, ProductType.Unknown);
        var withVegetable = FoodQualityScore.Calculate(100, 5, 2, 10, 3, 0, ProductType.Vegetable);

        Assert.True(withVegetable.Score > withoutType.Score);
    }

    [Fact]
    public void FoodQualityScore_Calculate_DessertModifier_DecreasesScore() {
        var withoutType = FoodQualityScore.Calculate(300, 5, 10, 40, 1, 0, ProductType.Unknown);
        var withDessert = FoodQualityScore.Calculate(300, 5, 10, 40, 1, 0, ProductType.Dessert);

        Assert.True(withDessert.Score < withoutType.Score);
    }

    [Theory]
    [InlineData(ProductType.Fruit)]
    [InlineData(ProductType.Seafood)]
    [InlineData(ProductType.Meat)]
    [InlineData(ProductType.Dairy)]
    [InlineData(ProductType.Grain)]
    public void FoodQualityScore_Calculate_PositiveProductTypeModifiers_IncreaseScore(ProductType productType) {
        var withoutType = FoodQualityScore.Calculate(180, 8, 4, 20, 3, 0, ProductType.Unknown);
        var withType = FoodQualityScore.Calculate(180, 8, 4, 20, 3, 0, productType);

        Assert.True(withType.Score > withoutType.Score);
    }

    [Theory]
    [InlineData(ProductType.Cheese)]
    [InlineData(ProductType.Beverage)]
    public void FoodQualityScore_Calculate_NegativeProductTypeModifiers_DecreaseScore(ProductType productType) {
        var withoutType = FoodQualityScore.Calculate(180, 8, 4, 20, 3, 0, ProductType.Unknown);
        var withType = FoodQualityScore.Calculate(180, 8, 4, 20, 3, 0, productType);

        Assert.True(withType.Score < withoutType.Score);
    }

    [Fact]
    public void FoodQualityScore_Calculate_ScoreIsClampedTo0_100() {
        var result = FoodQualityScore.Calculate(
            caloriesPerBase: 10, proteinsPerBase: 50, fatsPerBase: 0,
            carbsPerBase: 0, fiberPerBase: 50, alcoholPerBase: 0,
            productType: ProductType.Vegetable);

        Assert.InRange(result.Score, 0, 100);
    }

}
