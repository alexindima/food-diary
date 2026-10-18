using FoodDiary.Modules.Recipes.Domain.Entities;
using FoodDiary.Modules.Recipes.Domain.ValueObjects;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Recipes.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class RecipeTimingUpdateTests {
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void UpdateTimingAndServings_ExplicitClear_RemovesOnlySelectedTimes(bool clearPrep, bool clearCook) {
        var recipe = Recipe.Create(UserId.New(), "Original", servings: 2, prepTime: 12, cookTime: 15);

        recipe.UpdateTimingAndServings(clearPrepTime: clearPrep, clearCookTime: clearCook);

        Assert.Multiple(
            () => Assert.Equal(clearPrep ? null : (int?)12, recipe.PrepTime),
            () => Assert.Equal(clearCook ? null : (int?)15, recipe.CookTime),
            () => Assert.Equal(2, recipe.Servings),
            () => Assert.NotNull(recipe.ModifiedOnUtc));
    }

    [Fact]
    public void UpdateTimingAndServings_RepeatedClear_DoesNotChangeModificationTime() {
        var recipe = Recipe.Create(UserId.New(), "Original", servings: 2, prepTime: 12, cookTime: 15);
        recipe.UpdateTimingAndServings(clearPrepTime: true, clearCookTime: true);
        DateTime? modified = recipe.ModifiedOnUtc;

        recipe.UpdateTimingAndServings(clearPrepTime: true, clearCookTime: true);

        Assert.Equal(modified, recipe.ModifiedOnUtc);
    }

    [Fact]
    public void UpdateTimingAndServings_ClearUnknownTimes_IsNoOp() {
        var recipe = Recipe.Create(UserId.New(), "Original", servings: 2);

        recipe.UpdateTimingAndServings(clearPrepTime: true, clearCookTime: true);

        Assert.Null(recipe.ModifiedOnUtc);
    }

    [Theory]
    [InlineData(true, false, 0, null)]
    [InlineData(false, true, null, 15)]
    public void UpdateTimingAndServings_ConflictingClear_IsAtomic(bool clearPrep, bool clearCook, int? prep, int? cook) {
        var recipe = Recipe.Create(UserId.New(), "Original", servings: 2, prepTime: 12, cookTime: 15);

        Assert.Throws<ArgumentException>(() => recipe.UpdateTimingAndServings(
            prep, cook, servings: 3, clearPrepTime: clearPrep, clearCookTime: clearCook));

        Assert.Multiple(
            () => Assert.Equal(12, recipe.PrepTime),
            () => Assert.Equal(15, recipe.CookTime),
            () => Assert.Equal(2, recipe.Servings),
            () => Assert.Null(recipe.ModifiedOnUtc));
    }

    [Theory]
    [InlineData(true, false, 0, null)]
    [InlineData(false, true, null, 15)]
    public void Update_ConflictingTimingClear_DoesNotPartiallyApplyIdentity(bool clearPrep, bool clearCook, int? prep, int? cook) {
        var recipe = Recipe.Create(UserId.New(), "Original", servings: 2, prepTime: 12, cookTime: 15);

        Assert.Throws<ArgumentException>(() => recipe.Update(new RecipeUpdate(Name: "Changed", PrepTime: prep, CookTime: cook) {
            ClearPrepTime = clearPrep,
            ClearCookTime = clearCook,
        }));

        Assert.Multiple(
            () => Assert.Equal("Original", recipe.Name),
            () => Assert.Equal(12, recipe.PrepTime),
            () => Assert.Equal(15, recipe.CookTime),
            () => Assert.Null(recipe.ModifiedOnUtc));
    }

    [Fact]
    public void Update_ClearTimes_AppliesCompatibilityUpdate() {
        var recipe = Recipe.Create(UserId.New(), "Original", servings: 2, prepTime: 12, cookTime: 15);

        recipe.Update(new RecipeUpdate(Name: "Changed") { ClearPrepTime = true, ClearCookTime = true });

        Assert.Multiple(
            () => Assert.Equal("Changed", recipe.Name),
            () => Assert.Null(recipe.PrepTime),
            () => Assert.Null(recipe.CookTime),
            () => Assert.Equal(2, recipe.Servings));
    }

    [Fact]
    public void UpdateTimingAndServings_InvalidServings_DoesNotPartiallyClearTimes() {
        var recipe = Recipe.Create(UserId.New(), "Original", servings: 2, prepTime: 12, cookTime: 15);

        Assert.Throws<ArgumentOutOfRangeException>(() => recipe.UpdateTimingAndServings(
            servings: 0, clearPrepTime: true, clearCookTime: true));

        Assert.Multiple(
            () => Assert.Equal(12, recipe.PrepTime),
            () => Assert.Equal(15, recipe.CookTime),
            () => Assert.Equal(2, recipe.Servings),
            () => Assert.Null(recipe.ModifiedOnUtc));
    }
}
