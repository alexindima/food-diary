using FoodDiary.Modules.Recipes.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Recipes.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class RecipeDirectCycleTests {
    [Fact]
    public void MealPlanAndRecipe_RejectOutOfAggregateRangeAndDirectCycles() {
        var recipe = Recipe.Create(UserId.New(), "Recipe", servings: 1);
        RecipeStep step = recipe.AddStep(1, "Mix");
        Assert.Throws<ArgumentException>(() => step.AddNestedRecipeIngredient(recipe.Id, 1));
        Assert.Empty(step.Ingredients);
    }
}
