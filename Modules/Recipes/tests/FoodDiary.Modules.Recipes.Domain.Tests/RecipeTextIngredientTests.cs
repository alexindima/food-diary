using FoodDiary.Modules.Recipes.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Recipes.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class RecipeTextIngredientTests {
    [Fact]
    public void AddTextIngredient_NormalizesTextWithoutCreatingReferences() {
        var recipe = Recipe.Create(UserId.New(), "Soup", 2);
        RecipeIngredient ingredient = recipe.AddStep(1, "Season").AddTextIngredient("  Salt  ", " to taste ");
        Assert.Multiple(
            () => Assert.Equal("Salt", ingredient.TextName),
            () => Assert.Equal("to taste", ingredient.AmountText),
            () => Assert.Null(ingredient.ProductId),
            () => Assert.Null(ingredient.NestedRecipeId),
            () => Assert.Equal(0, ingredient.Amount));
    }

    [Fact]
    public void AddTextIngredient_AllowsMissingAmountAndRejectsInvalidNames() {
        RecipeStep step = Recipe.Create(UserId.New(), "Soup", 2).AddStep(1, "Season");
        Assert.Null(step.AddTextIngredient("Salt").AmountText);
        Assert.Throws<ArgumentException>(() => step.AddTextIngredient(" "));
        Assert.Throws<ArgumentException>(() => step.AddTextIngredient(new string('a', 257)));
        Assert.Throws<ArgumentException>(() => step.AddTextIngredient("Salt", new string('a', 129)));
    }
}
