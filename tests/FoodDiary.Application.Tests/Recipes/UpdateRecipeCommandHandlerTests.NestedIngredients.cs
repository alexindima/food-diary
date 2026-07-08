using FoodDiary.Results;
using FoodDiary.Application.Recipes.Commands.UpdateRecipe;
using FoodDiary.Application.Recipes.Models;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Recipes;

public partial class UpdateRecipeCommandHandlerTests {

    [Fact]
    public async Task Handle_WithSelfNestedRecipeIngredient_ReturnsValidationFailure() {
        var userId = UserId.New();
        var recipeId = RecipeId.New();
        var recipe = Recipe.Create(userId, "Soup", servings: 2);
        recipe.AddStep(1, "Initial step");

        UpdateRecipeCommandHandler handler = UpdateRecipeHandler(
            CreateRecipeRepository(recipeId, userId, recipe),
            new NoopImageAssetCleanupService(),
            CreateUserRepository(User.Create("user@example.com", "hash")),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance,
            new AllowAllProductLookupService(),
            new AllowAllRecipeLookupService());

        Result<RecipeModel> result = await handler.Handle(
            new UpdateRecipeCommand(
                userId.Value,
                recipeId.Value,
                Name: "Soup",
                Description: null,
                ClearDescription: false,
                Comment: null,
                ClearComment: false,
                Category: null,
                ClearCategory: false,
                ImageUrl: null,
                ClearImageUrl: false,
                ImageAssetId: null,
                ClearImageAssetId: false,
                PrepTime: 0,
                CookTime: 20,
                Servings: 2,
                Visibility: Visibility.Public.ToString(),
                CalculateNutritionAutomatically: true,
                ManualCalories: null,
                ManualProteins: null,
                ManualFats: null,
                ManualCarbs: null,
                ManualFiber: null,
                ManualAlcohol: null,
                Steps: [CreateStepWithNestedRecipe(order: 1, "Initial step", recipeId.Value)]),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("itself", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_WithNestedRecipeIngredient_AddsNestedRecipeIngredient() {
        var userId = UserId.New();
        var recipeId = RecipeId.New();
        var nestedRecipeId = RecipeId.New();
        var recipe = Recipe.Create(userId, "Soup", servings: 2);
        SetRecipeId(recipe, recipeId);
        recipe.AddStep(1, "Initial step");
        UpdateRecipeCommandHandler handler = UpdateRecipeHandler(
            CreateRecipeRepository(recipeId, userId, recipe),
            new NoopImageAssetCleanupService(),
            CreateUserRepository(User.Create("nested-update@example.com", "hash")),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance,
            new AllowAllProductLookupService(),
            new AllowAllRecipeLookupService());

        Result<RecipeModel> result = await handler.Handle(
            UpdateCommand(
                userId.Value,
                recipeId.Value,
                steps: [CreateStepWithNestedRecipe(order: 1, "Updated step", nestedRecipeId.Value)]),
            CancellationToken.None);

        ResultAssert.Success(result);
        RecipeIngredient ingredient = Assert.Single(Assert.Single(recipe.Steps).Ingredients);
        Assert.Equal(nestedRecipeId, ingredient.NestedRecipeId);
    }
}
