using FoodDiary.Modules.Recipes.Domain.Entities;
using FoodDiary.Modules.Recipes.Domain.ValueObjects;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Recipes.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class RecipeUpdateAtomicityTests {
    [Fact]
    public void CompositeUpdates_WhenLateValidationFails_AreAtomic() {
        var recipe = Recipe.Create(UserId.New(), "Original", servings: 1);

        Assert.Throws<ArgumentOutOfRangeException>(() => recipe.Update(new RecipeUpdate(
            Name: "Changed",
            ImageUrl: new string('x', 2049))));

        Assert.Multiple(
            () => Assert.Equal("Original", recipe.Name),
            () => Assert.Null(recipe.ModifiedOnUtc));
    }
}
