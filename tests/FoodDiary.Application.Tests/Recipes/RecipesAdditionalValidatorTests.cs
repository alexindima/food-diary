using FluentValidation.TestHelper;
using FoodDiary.Application.Recipes.Commands.DuplicateRecipe;
using FoodDiary.Application.Recipes.Queries.GetRecipeById;
using FoodDiary.Application.Recipes.Queries.GetRecipes;
using FoodDiary.Application.Recipes.Queries.GetRecentRecipes;
using FoodDiary.Application.Recipes.Queries.GetRecipesOverview;

namespace FoodDiary.Application.Tests.Recipes;

[ExcludeFromCodeCoverage]
public class RecipesAdditionalValidatorTests {
    // â”€â”€ DuplicateRecipe â”€â”€

    [Fact]
    public async Task DuplicateRecipe_WithNullUserId_HasError() {
        TestValidationResult<DuplicateRecipeCommand> result = await new DuplicateRecipeCommandValidator().TestValidateAsync(
            new DuplicateRecipeCommand(null, Guid.NewGuid()));
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public async Task DuplicateRecipe_WithEmptyRecipeId_HasError() {
        TestValidationResult<DuplicateRecipeCommand> result = await new DuplicateRecipeCommandValidator().TestValidateAsync(
            new DuplicateRecipeCommand(Guid.NewGuid(), Guid.Empty));
        result.ShouldHaveValidationErrorFor(c => c.RecipeId);
    }

    // â”€â”€ GetRecipeById â”€â”€

    [Fact]
    public async Task GetRecipeById_WithNullUserId_HasError() {
        TestValidationResult<GetRecipeByIdQuery> result = await new GetRecipeByIdQueryValidator().TestValidateAsync(
            new GetRecipeByIdQuery(null, Guid.NewGuid(), false));
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public async Task GetRecipeById_WithEmptyRecipeId_HasError() {
        TestValidationResult<GetRecipeByIdQuery> result = await new GetRecipeByIdQueryValidator().TestValidateAsync(
            new GetRecipeByIdQuery(Guid.NewGuid(), Guid.Empty, false));
        result.ShouldHaveValidationErrorFor(c => c.RecipeId);
    }

    // â”€â”€ GetRecipes â”€â”€

    [Fact]
    public async Task GetRecipes_WithZeroPage_HasError() {
        TestValidationResult<GetRecipesQuery> result = await new GetRecipesQueryValidator().TestValidateAsync(
            new GetRecipesQuery(Guid.NewGuid(), 0, 10, null, false));
        result.ShouldHaveValidationErrorFor(c => c.Page);
    }

    [Fact]
    public async Task GetRecipes_WithZeroLimit_HasError() {
        TestValidationResult<GetRecipesQuery> result = await new GetRecipesQueryValidator().TestValidateAsync(
            new GetRecipesQuery(Guid.NewGuid(), 1, 0, null, false));
        result.ShouldHaveValidationErrorFor(c => c.Limit);
    }

    // â”€â”€ GetRecentRecipes â”€â”€

    [Fact]
    public async Task GetRecentRecipes_WithNullUserId_HasError() {
        TestValidationResult<GetRecentRecipesQuery> result = await new GetRecentRecipesQueryValidator().TestValidateAsync(
            new GetRecentRecipesQuery(null, 10, false));
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    // â”€â”€ GetRecipesWithRecent â”€â”€

    [Fact]
    public async Task GetRecipesOverview_WithNullUserId_HasError() {
        TestValidationResult<GetRecipesOverviewQuery> result = await new GetRecipesOverviewQueryValidator().TestValidateAsync(
            new GetRecipesOverviewQuery(null, 1, 10, null, false));
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }
}
