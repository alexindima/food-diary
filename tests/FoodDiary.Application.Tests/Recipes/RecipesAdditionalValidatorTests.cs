using FluentValidation.TestHelper;
using FoodDiary.Application.Recipes.Commands.DuplicateRecipe;
using FoodDiary.Application.Recipes.Queries.GetRecipeById;
using FoodDiary.Application.Recipes.Queries.GetRecipes;
using FoodDiary.Application.Recipes.Queries.GetRecentRecipes;
using FoodDiary.Application.Recipes.Queries.GetRecipesWithRecent;

namespace FoodDiary.Application.Tests.Recipes;

public class RecipesAdditionalValidatorTests {
    // ── DuplicateRecipe ──

    [Fact]
    public async Task DuplicateRecipe_WithNullUserId_HasError() {
        var result = await new DuplicateRecipeCommandValidator().TestValidateAsync(
            new DuplicateRecipeCommand(null, Guid.NewGuid()));
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public async Task DuplicateRecipe_WithEmptyRecipeId_HasError() {
        var result = await new DuplicateRecipeCommandValidator().TestValidateAsync(
            new DuplicateRecipeCommand(Guid.NewGuid(), Guid.Empty));
        result.ShouldHaveValidationErrorFor(c => c.RecipeId);
    }

    // ── GetRecipeById ──

    [Fact]
    public async Task GetRecipeById_WithNullUserId_HasError() {
        var result = await new GetRecipeByIdQueryValidator().TestValidateAsync(
            new GetRecipeByIdQuery(null, Guid.NewGuid(), false));
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public async Task GetRecipeById_WithEmptyRecipeId_HasError() {
        var result = await new GetRecipeByIdQueryValidator().TestValidateAsync(
            new GetRecipeByIdQuery(Guid.NewGuid(), Guid.Empty, false));
        result.ShouldHaveValidationErrorFor(c => c.RecipeId);
    }

    // ── GetRecipes ──

    [Fact]
    public async Task GetRecipes_WithZeroPage_HasError() {
        var result = await new GetRecipesQueryValidator().TestValidateAsync(
            new GetRecipesQuery(Guid.NewGuid(), 0, 10, null, false));
        result.ShouldHaveValidationErrorFor(c => c.Page);
    }

    [Fact]
    public async Task GetRecipes_WithZeroLimit_HasError() {
        var result = await new GetRecipesQueryValidator().TestValidateAsync(
            new GetRecipesQuery(Guid.NewGuid(), 1, 0, null, false));
        result.ShouldHaveValidationErrorFor(c => c.Limit);
    }

    // ── GetRecentRecipes ──

    [Fact]
    public async Task GetRecentRecipes_WithNullUserId_HasError() {
        var result = await new GetRecentRecipesQueryValidator().TestValidateAsync(
            new GetRecentRecipesQuery(null, 10, false));
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    // ── GetRecipesWithRecent ──

    [Fact]
    public async Task GetRecipesWithRecent_WithNullUserId_HasError() {
        var result = await new GetRecipesWithRecentQueryValidator().TestValidateAsync(
            new GetRecipesWithRecentQuery(null, 1, 10, null, false));
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }
}
