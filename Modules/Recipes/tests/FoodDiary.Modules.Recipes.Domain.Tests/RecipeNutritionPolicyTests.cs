using FoodDiary.Modules.Recipes.Domain.Nutrition;

namespace FoodDiary.Modules.Recipes.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class RecipeNutritionPolicyTests {
    [Fact]
    public void Calculate_PreservesStoredValuesWithoutUsableSources() {
        var stored = new RecipeNutritionValues(TotalCalories: 100, TotalProteins: 2, TotalFats: 3, TotalCarbs: 4, TotalFiber: 5, TotalAlcohol: null);
        Assert.Same(stored, RecipeNutritionPolicy.Calculate([], stored));
        Assert.Same(stored, RecipeNutritionPolicy.Calculate(
            [new RecipeNutritionIngredient(10, 0, stored, NestedRecipeServings: null, NestedRecipe: null)], stored));
    }

    [Fact]
    public void Calculate_UsesProductBeforeNestedRecipeAndRoundsCombinedTotal() {
        var source = new RecipeNutritionValues(TotalCalories: 10.125, TotalProteins: 1, TotalFats: 2, TotalCarbs: 3, TotalFiber: 4, TotalAlcohol: 5);
        var nested = new RecipeNutritionValues(TotalCalories: 500, TotalProteins: 500, TotalFats: 500, TotalCarbs: 500, TotalFiber: 500, TotalAlcohol: 500);
        RecipeNutritionValues result = RecipeNutritionPolicy.Calculate(
            [new RecipeNutritionIngredient(1, 1, source, 1, nested),
             new RecipeNutritionIngredient(1, 1, source, NestedRecipeServings: null, NestedRecipe: null)], nested);
        Assert.Equal(new RecipeNutritionValues(TotalCalories: 20.25, TotalProteins: 2, TotalFats: 4, TotalCarbs: 6, TotalFiber: 8, TotalAlcohol: 10), result);
    }

    [Fact]
    public void Calculate_ScalesNestedServingAndKeepsZeroNutritionAsComputed() {
        var stored = new RecipeNutritionValues(TotalCalories: 999, TotalProteins: 999, TotalFats: 999, TotalCarbs: 999, TotalFiber: 999, TotalAlcohol: 999);
        RecipeNutritionValues result = RecipeNutritionPolicy.Calculate(
            [new RecipeNutritionIngredient(1, ProductBaseAmount: null, Product: null, NestedRecipeServings: 2, NestedRecipe: new RecipeNutritionValues(TotalCalories: 200, TotalProteins: 20, TotalFats: 10, TotalCarbs: 30, TotalFiber: 8, TotalAlcohol: null))], stored);
        Assert.Equal(new RecipeNutritionValues(TotalCalories: 100, TotalProteins: 10, TotalFats: 5, TotalCarbs: 15, TotalFiber: 4, TotalAlcohol: 0), result);
        Assert.Equal(new RecipeNutritionValues(TotalCalories: 0, TotalProteins: 0, TotalFats: 0, TotalCarbs: 0, TotalFiber: 0, TotalAlcohol: 0), RecipeNutritionPolicy.Calculate(
            [new RecipeNutritionIngredient(1, 100, new RecipeNutritionValues(TotalCalories: null, TotalProteins: null, TotalFats: null, TotalCarbs: null, TotalFiber: null, TotalAlcohol: null), NestedRecipeServings: null, NestedRecipe: null)], stored));
    }

    [Fact]
    public void SelectManual_FallsBackPerNutrientAndPreservesExplicitZero() {
        RecipeNutritionValues result = RecipeNutritionPolicy.SelectManual(
            new RecipeNutritionValues(TotalCalories: 0, TotalProteins: null, TotalFats: 5, TotalCarbs: null, TotalFiber: 0, TotalAlcohol: null), new RecipeNutritionValues(TotalCalories: 100, TotalProteins: 10, TotalFats: 20, TotalCarbs: 30, TotalFiber: 40, TotalAlcohol: null));
        Assert.Equal(new RecipeNutritionValues(TotalCalories: 0, TotalProteins: 10, TotalFats: 5, TotalCarbs: 30, TotalFiber: 0, TotalAlcohol: null), result);
    }
}
