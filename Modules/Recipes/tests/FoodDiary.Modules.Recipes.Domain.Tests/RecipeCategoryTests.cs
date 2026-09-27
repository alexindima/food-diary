using FoodDiary.Modules.Recipes.Domain.Contracts.Enums;
using FoodDiary.Modules.Recipes.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Recipes.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class RecipeCategoryTests {
    [Fact]
    public void EveryCategory_RoundTripsThroughItsStableCode() {
        Assert.Equal(15, RecipeCategoryCodes.All.Count);
        foreach (RecipeCategory category in Enum.GetValues<RecipeCategory>()) {
            string code = category.ToCode();
            Assert.True(RecipeCategoryCodes.IsValid(code));
            Assert.Equal(category, RecipeCategoryCodes.Parse(code));
            Assert.Equal(category, Recipe.Create(UserId.New(), "Recipe", 1, category: code).Category);
        }
    }

    [Fact]
    public void MissingOrClearedCategory_UsesOther() {
        var recipe = Recipe.Create(UserId.New(), "Recipe", 1);
        Assert.Equal(RecipeCategory.Other, recipe.Category);
        recipe.UpdateIdentity(category: "salads");
        Assert.Equal(RecipeCategory.Salads, recipe.Category);
        recipe.UpdateIdentity(clearCategory: true);
        Assert.Equal(RecipeCategory.Other, recipe.Category);
    }

    [Theory]
    [InlineData("Dinner")]
    [InlineData("Салаты")]
    [InlineData("")]
    [InlineData("42")]
    public void UnknownCategory_IsRejectedWithoutChangingRecipe(string code) {
        var recipe = Recipe.Create(UserId.New(), "Recipe", 1, category: "soups");
        Assert.Throws<ArgumentOutOfRangeException>(() => recipe.UpdateIdentity(name: "Changed", category: code));
        Assert.Equal("Recipe", recipe.Name);
        Assert.Equal(RecipeCategory.Soups, recipe.Category);
    }
}
