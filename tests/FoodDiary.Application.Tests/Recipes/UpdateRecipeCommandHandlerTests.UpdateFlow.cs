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
    public async Task Handle_WithBlankVisibility_DoesNotChangeRecipeVisibility() {
        var userId = UserId.New();
        var recipeId = RecipeId.New();
        var recipe = Recipe.Create(userId, "Soup", servings: 2, visibility: Visibility.Private);
        SetRecipeId(recipe, recipeId);
        recipe.AddStep(1, "Initial step");
        UpdateRecipeCommandHandler handler = UpdateRecipeHandler(
            CreateRecipeRepository(recipeId, userId, recipe),
            new NoopImageAssetCleanupService(),
            CreateUserRepository(User.Create("blank-visibility@example.com", "hash")),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance,
            new AllowAllProductLookupService(),
            new AllowAllRecipeLookupService());

        Result<RecipeModel> result = await handler.Handle(UpdateCommand(userId.Value, recipeId.Value, visibility: " "), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(Visibility.Private, recipe.Visibility);
    }

    [Fact]
    public async Task Handle_WhenPrepTimeOmitted_PreservesExistingPrepTime() {
        var userId = UserId.New();
        var recipeId = RecipeId.New();
        var recipe = Recipe.Create(userId, "Soup", servings: 2, prepTime: 15);
        SetRecipeId(recipe, recipeId);
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
                PrepTime: null,
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
                Steps: [CreateStep(order: 1, "Initial step")]),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(15, recipe.PrepTime);
    }

    [Fact]
    public async Task Handle_WhenUpdatedRecipeCannotBeReloaded_ReturnsInvalidData() {
        var userId = UserId.New();
        var recipeId = RecipeId.New();
        var recipe = Recipe.Create(userId, "Soup", servings: 2);
        SetRecipeId(recipe, recipeId);
        recipe.AddStep(1, "Initial step");
        var repository = new ReloadMissingRecipeRepository(recipeId, userId, recipe);
        UpdateRecipeCommandHandler handler = UpdateRecipeHandler(
            repository,
            new NoopImageAssetCleanupService(),
            CreateUserRepository(User.Create("user@example.com", "hash")),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance,
            new AllowAllProductLookupService(),
            new AllowAllRecipeLookupService());

        Result<RecipeModel> result = await handler.Handle(
            new UpdateRecipeCommand(
                userId.Value,
                recipeId.Value,
                Name: "Updated soup",
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
                PrepTime: null,
                CookTime: 20,
                Servings: 2,
                Visibility: Visibility.Public.ToString(),
                CalculateNutritionAutomatically: false,
                ManualCalories: 100,
                ManualProteins: 10,
                ManualFats: 4,
                ManualCarbs: 20,
                ManualFiber: 2,
                ManualAlcohol: null,
                Steps: [CreateStep(order: 1, "Updated step")]),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Recipe.InvalidData", result.Error.Code);
        Assert.True(repository.UpdateCalled);
    }
}
