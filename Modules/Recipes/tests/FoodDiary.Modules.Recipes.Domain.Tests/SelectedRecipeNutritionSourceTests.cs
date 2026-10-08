using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects;
using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects;
using FoodDiary.Modules.Recipes.Domain.Nutrition;

namespace FoodDiary.Modules.Recipes.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class SelectedRecipeNutritionSourceTests {
    [Fact]
    public void TypedSources_KeepProductBaseAndRecipeYieldDistinct() {
        var productNutrition = new RecipeNutritionValues(80, 4, 2, 10, 1, 0);
        var recipeNutrition = new RecipeNutritionValues(333.33, 12, 6, 30, 4, 0);
        var product = RecipeNutritionIngredient.FromProduct(ProductUnitQuantity.FromUnits(250), 100, productNutrition);
        var recipe = RecipeNutritionIngredient.FromRecipe(RecipeServingQuantity.FromServings(0.5), 2, recipeNutrition);

        RecipeNutritionValues result = RecipeNutritionPolicy.Calculate([product, recipe], productNutrition);

        Assert.Multiple(() => {
            Assert.Equal(RecipeNutritionSource.Product, product.Kind);
            Assert.Equal(RecipeNutritionSource.Recipe, recipe.Kind);
            Assert.Equal(283.33, result.TotalCalories);
            Assert.Equal(13, result.TotalProteins);
            Assert.Equal(6.5, result.TotalFats);
            Assert.Equal(32.5, result.TotalCarbs);
            Assert.Equal(3.5, result.TotalFiber);
        });
    }

    [Fact]
    public void StoredSources_PreserveProductPrecedenceEvenWhenItsNutritionIsMissing() {
        var nested = new RecipeNutritionValues(500, 20, 10, 30, 4, 0);
        var ingredient = RecipeNutritionIngredient.FromStoredSources(1, 100, product: null, 2, nested);

        Assert.Equal(RecipeNutritionSource.Product, ingredient.Kind);
        Assert.Null(ingredient.Nutrition);
        Assert.Equal(0, RecipeNutritionPolicy.Calculate([ingredient], nested).TotalCalories);
        Assert.Null(RecipeNutritionPolicy.Calculate([ingredient], nested, hasUncalculatedIngredients: true).TotalCalories);
    }

    [Fact]
    public void MissingBasis_PreservesStoredFallbackAndIncompleteDistinction() {
        var stored = new RecipeNutritionValues(100, 2, 3, 4, 5, 0);
        var missing = RecipeNutritionIngredient.FromStoredSources(0, 0, stored, nestedRecipeServings: null, nestedRecipe: null);

        Assert.Same(RecipeNutritionIngredient.Unavailable, missing);
        Assert.Same(stored, RecipeNutritionPolicy.Calculate([missing], stored));
        Assert.Null(RecipeNutritionPolicy.Calculate([missing], stored, hasUncalculatedIngredients: true).TotalCalories);
    }
}
