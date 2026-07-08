using FoodDiary.Results;
using FoodDiary.Application.Recipes.Commands.UpdateRecipe;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FluentValidation.Results;
using FoodDiary.Application.Recipes.Models;

namespace FoodDiary.Application.Tests.Recipes;

public partial class RecipesFeatureTests {

    [Fact]
    public async Task UpdateRecipeCommandValidator_WithConflictingClearFlagsDuplicateStepsAndMissingManualNutrition_ReturnsErrors() {
        var userId = UserId.New();
        var recipe = Recipe.Create(userId, "Soup", servings: 2);
        var validator = new UpdateRecipeCommandValidator();

        ValidationResult result = await validator.ValidateAsync(new UpdateRecipeCommand(
            userId.Value,
            recipe.Id.Value,
            Name: "Soup",
            Description: "description",
            ClearDescription: true,
            Comment: "comment",
            ClearComment: true,
            Category: "category",
            ClearCategory: true,
            ImageUrl: "https://cdn.test/soup.png",
            ClearImageUrl: true,
            ImageAssetId: ImageAssetId.New().Value,
            ClearImageAssetId: true,
            PrepTime: -1,
            CookTime: 0,
            Servings: 0,
            Visibility: "unknown",
            CalculateNutritionAutomatically: false,
            ManualCalories: null,
            ManualProteins: null,
            ManualFats: null,
            ManualCarbs: null,
            ManualFiber: null,
            ManualAlcohol: -1,
            Steps: [
                CreateRecipeCreateStep(order: 1, "Mix"),
                CreateRecipeCreateStep(order: 1, "Serve"),
            ]));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => string.Equals(error.ErrorCode, "Validation.Invalid", StringComparison.Ordinal));
        Assert.Contains(result.Errors, error => error.ErrorMessage.IndexOf("Step order", StringComparison.Ordinal) >= 0);
        Assert.Contains(result.Errors, error => error.ErrorMessage.IndexOf("Manual nutrition", StringComparison.Ordinal) >= 0);
    }


    [Fact]
    public async Task UpdateRecipeCommandHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-update-recipe@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var recipe = Recipe.Create(user.Id, "Soup", servings: 2, visibility: Visibility.Private);
        UpdateRecipeCommandHandler handler = UpdateRecipeHandler(
            new SingleRecipeRepository(recipe),
            new RecordingCleanupService(),
            new StubUserRepository(user),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance,
            new AllowAllProductLookupService(),
            new AllowAllRecipeLookupService());

        Result<RecipeModel> result = await handler.Handle(
            new UpdateRecipeCommand(
                user.Id.Value,
                recipe.Id.Value,
                Name: "Updated Soup",
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
                CookTime: null,
                Servings: 2,
                Visibility: Visibility.Private.ToString(),
                CalculateNutritionAutomatically: true,
                ManualCalories: null,
                ManualProteins: null,
                ManualFats: null,
                ManualCarbs: null,
                ManualFiber: null,
                ManualAlcohol: null,
                Steps: []),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }


    [Fact]
    public async Task UpdateRecipeCommandHandler_WithoutImageChange_DoesNotCleanupExistingRecipeAsset() {
        var user = User.Create("recipe-owner@example.com", "hash");
        var recipeAssetId = ImageAssetId.New();
        var recipe = Recipe.Create(
            user.Id,
            "Soup",
            servings: 2,
            imageAssetId: recipeAssetId,
            visibility: Visibility.Private);

        var cleanup = new RecordingCleanupService();
        UpdateRecipeCommandHandler handler = UpdateRecipeHandler(
            new SingleRecipeRepository(recipe),
            cleanup,
            new StubUserRepository(user),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance,
            new AllowAllProductLookupService(),
            new AllowAllRecipeLookupService());

        Result<RecipeModel> result = await handler.Handle(
            new UpdateRecipeCommand(
                user.Id.Value,
                recipe.Id.Value,
                Name: "Updated Soup",
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
                CookTime: null,
                Servings: 2,
                Visibility: Visibility.Private.ToString(),
                CalculateNutritionAutomatically: true,
                ManualCalories: null,
                ManualProteins: null,
                ManualFats: null,
                ManualCarbs: null,
                ManualFiber: null,
                ManualAlcohol: null,
                Steps: []),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Empty(cleanup.RequestedAssetIds);
    }

}
