using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects;

namespace FoodDiary.Modules.Recipes.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class RecipeServingQuantityTests {
    [Theory]
    [InlineData(0.000000001)]
    [InlineData(0.125)]
    [InlineData(1.333333333)]
    [InlineData(1000000)]
    public void FromServings_PreservesFractionalServings(double servings) {
        var quantity = RecipeServingQuantity.FromServings(servings);
        Assert.Equal(servings, quantity.Value);
        Assert.Equal(quantity, RecipeServingQuantity.FromServings(servings));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1000000.000001)]
    [InlineData(double.MaxValue)]
    [InlineData(double.NaN)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(double.PositiveInfinity)]
    public void FromServings_RejectsInvalidMutationQuantities(double servings) {
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => RecipeServingQuantity.FromServings(servings));
        Assert.Equal("servings", error.ParamName);
    }
}
