using FoodDiary.Modules.Recipes.Domain.Nutrition;
using FsCheck;
using FsCheck.Xunit;

namespace FoodDiary.Modules.Recipes.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class RecipeNutritionPropertyTests {
    private static readonly RecipeNutritionValues Empty = new(TotalCalories: null, TotalProteins: null, TotalFats: null, TotalCarbs: null, TotalFiber: null, TotalAlcohol: null);

    [Property(MaxTest = 300)]
    public void SplittingIngredient_PreservesCombinedNutrition(PositiveInt left, PositiveInt right, NonNegativeInt nutrient) {
        // Binary fractions keep the independent equivalence away from rounding ties.
        double first = ((left.Get % 1000) + 1) / 4d;
        double second = ((right.Get % 1000) + 1) / 4d;
        double value = (nutrient.Get % 1000) / 4d;
        var source = new RecipeNutritionValues(TotalCalories: value, TotalProteins: value, TotalFats: value, TotalCarbs: value, TotalFiber: value, TotalAlcohol: value);
        RecipeNutritionValues split = RecipeNutritionPolicy.Calculate(
            [RecipeNutritionIngredient.FromStoredProduct(first, 128, source),
             RecipeNutritionIngredient.FromStoredProduct(second, 128, source)], Empty);
        RecipeNutritionValues combined = RecipeNutritionPolicy.Calculate(
            [RecipeNutritionIngredient.FromStoredProduct(first + second, 128, source)], Empty);
        Assert.Equal(combined, split);
    }

    [Property(MaxTest = 300)]
    public void ProductAndNestedRecipe_WithEqualBasisHaveEqualNutrition(PositiveInt quantity, PositiveInt basis, NonNegativeInt nutrient) {
        double amount = ((quantity.Get % 1000) + 1) / 4d;
        double divisor = (basis.Get % 100) + 1;
        double value = (nutrient.Get % 10000) / 4d;
        var source = new RecipeNutritionValues(TotalCalories: value, TotalProteins: value / 2, TotalFats: value / 4, TotalCarbs: value / 8, TotalFiber: null, TotalAlcohol: 0);
        RecipeNutritionValues product = RecipeNutritionPolicy.Calculate(
            [RecipeNutritionIngredient.FromStoredProduct(amount, divisor, source)], Empty);
        RecipeNutritionValues recipe = RecipeNutritionPolicy.Calculate(
            [RecipeNutritionIngredient.FromStoredRecipe(amount, divisor, source)], Empty);
        Assert.Equal(product, recipe);
    }

    [Property(MaxTest = 300)]
    public void UnavailableSources_DoNotChangeCalculatedTotals(PositiveInt amount, NonNegativeInt nutrient) {
        var source = new RecipeNutritionValues(TotalCalories: nutrient.Get % 10000, TotalProteins: 1, TotalFats: 2, TotalCarbs: 3, TotalFiber: 4, TotalAlcohol: 5);
        var ingredient = RecipeNutritionIngredient.FromStoredProduct((amount.Get % 1000) + 1, 100, source);
        RecipeNutritionValues expected = RecipeNutritionPolicy.Calculate([ingredient], Empty);
        Assert.Equal(expected, RecipeNutritionPolicy.Calculate(
            [RecipeNutritionIngredient.Unavailable, ingredient, RecipeNutritionIngredient.Unavailable], Empty));
    }

    [Property(MaxTest = 300)]
    public void ManualZero_OverridesEveryStoredNutrient(NonNegativeInt nutrient) {
        double value = nutrient.Get % 10000;
        var stored = new RecipeNutritionValues(TotalCalories: value, TotalProteins: value, TotalFats: value, TotalCarbs: value, TotalFiber: value, TotalAlcohol: value);
        var zero = new RecipeNutritionValues(TotalCalories: 0, TotalProteins: 0, TotalFats: 0, TotalCarbs: 0, TotalFiber: 0, TotalAlcohol: 0);
        Assert.Equal(zero, RecipeNutritionPolicy.SelectManual(zero, stored));
        Assert.Equal(stored, RecipeNutritionPolicy.SelectManual(Empty, stored));
    }

    [Theory]
    [InlineData(0.125, 0.12)]
    [InlineData(0.375, 0.38)]
    [InlineData(10.125, 10.12)]
    [InlineData(10.375, 10.38)]
    public void FinalTotals_RoundMidpointsToEven(double input, double expected) {
        var source = new RecipeNutritionValues(TotalCalories: input, TotalProteins: input, TotalFats: input, TotalCarbs: input, TotalFiber: input, TotalAlcohol: input);
        Assert.Equal(new RecipeNutritionValues(TotalCalories: expected, TotalProteins: expected, TotalFats: expected, TotalCarbs: expected, TotalFiber: expected, TotalAlcohol: expected),
            RecipeNutritionPolicy.Calculate([RecipeNutritionIngredient.FromStoredProduct(100, 100, source)], Empty));
    }

    [Fact]
    public void PartialRecipe_WithOnlyFiberAndAlcoholRetainsUnknownTotals() {
        var source = new RecipeNutritionValues(TotalCalories: null, TotalProteins: null, TotalFats: null, TotalCarbs: null, TotalFiber: 10, TotalAlcohol: 5);
        Assert.Equal(Empty, RecipeNutritionPolicy.Calculate(
            [RecipeNutritionIngredient.FromStoredProduct(100, 100, source)], Empty, hasUncalculatedIngredients: true));
        Assert.Equal(new RecipeNutritionValues(TotalCalories: 0, TotalProteins: 0, TotalFats: 0, TotalCarbs: 0, TotalFiber: 10, TotalAlcohol: 5), RecipeNutritionPolicy.Calculate(
            [RecipeNutritionIngredient.FromStoredProduct(100, 100, source)], Empty));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0d)]
    [InlineData(-1d)]
    public void MissingOrNonpositiveBasis_IsUnavailable(double? basis) {
        Assert.Same(RecipeNutritionIngredient.Unavailable, RecipeNutritionIngredient.FromStoredProduct(10, basis, Empty));
        Assert.Same(RecipeNutritionIngredient.Unavailable, RecipeNutritionIngredient.FromStoredRecipe(10, basis, Empty));
        Assert.Equal(RecipeNutritionSource.Recipe, RecipeNutritionIngredient.FromStoredSources(10, basis, Empty, 2, Empty).Kind);
    }

    [Fact]
    public void TypedQuantityFactories_RejectAbsentQuantitiesAtTheBoundary() {
        Assert.Throws<ArgumentNullException>(() => RecipeNutritionIngredient.FromProduct(null!, 100, Empty));
        Assert.Throws<ArgumentNullException>(() => RecipeNutritionIngredient.FromRecipe(null!, 2, Empty));
    }
}
