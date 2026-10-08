using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects;
using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects;
using FoodDiary.Modules.Recipes.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Recipes.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class TypedRecipeQuantityTests {
    [Fact]
    public void QuantityMutation_RejectsTheOppositeIngredientKindWithoutChangingState() {
        var recipe = Recipe.Create(UserId.New(), "Quantity test", 2);
        RecipeStep step = recipe.AddStep(1, "Mix");
        RecipeIngredient product = step.AddProductIngredient(ProductId.New(), ProductUnitQuantity.FromUnits(125.5));
        var nested = Recipe.Create(UserId.New(), "Nested", 2);
        RecipeIngredient nestedIngredient = step.AddNestedRecipeIngredient(nested.Id, RecipeServingQuantity.FromServings(0.75));
        RecipeIngredient text = step.AddTextIngredient("Salt", "to taste");

        Assert.Throws<InvalidOperationException>(() => product.UpdateRecipeServings(RecipeServingQuantity.FromServings(1)));
        Assert.Throws<InvalidOperationException>(() => nestedIngredient.UpdateProductQuantity(ProductUnitQuantity.FromUnits(250)));
        Assert.Throws<InvalidOperationException>(() => text.UpdateProductQuantity(ProductUnitQuantity.FromUnits(1)));
        Assert.Throws<InvalidOperationException>(() => text.UpdateRecipeServings(RecipeServingQuantity.FromServings(1)));

        Assert.Multiple(() => {
            Assert.Equal(125.5, product.Amount);
            Assert.Equal(0.75, nestedIngredient.Amount);
            Assert.Equal(0, text.Amount);
            Assert.Null(product.ModifiedOnUtc);
            Assert.Null(nestedIngredient.ModifiedOnUtc);
            Assert.Null(text.ModifiedOnUtc);
        });
    }

    [Fact]
    public void ServingMutation_PreservesFractionalAmountAndNoOpAudit() {
        var recipe = Recipe.Create(UserId.New(), "Quantity test", 2);
        var nested = Recipe.Create(UserId.New(), "Nested", 2);
        RecipeIngredient ingredient = recipe.AddStep(1, "Mix").AddNestedRecipeIngredient(nested.Id, RecipeServingQuantity.FromServings(0.75));
        ingredient.UpdateRecipeServings(RecipeServingQuantity.FromServings(0.75));
        Assert.Null(ingredient.ModifiedOnUtc);
        ingredient.UpdateRecipeServings(RecipeServingQuantity.FromServings(1.125));
        Assert.Equal(1.125, ingredient.Amount);
        Assert.NotNull(ingredient.ModifiedOnUtc);
    }
}
