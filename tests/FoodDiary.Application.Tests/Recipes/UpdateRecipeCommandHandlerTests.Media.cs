using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Recipes.Common;
using FoodDiary.Application.Recipes.Commands.UpdateRecipe;
using FoodDiary.Application.Recipes.Common;
using FoodDiary.Application.Recipes.Models;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Recipes;

public partial class UpdateRecipeCommandHandlerTests {

    [Fact]
    public async Task Handle_WithClearImageFlags_ClearsRecipeMedia() {
        var userId = UserId.New();
        var recipeId = RecipeId.New();
        var imageAssetId = ImageAssetId.New();
        var recipe = Recipe.Create(userId, "Soup", servings: 2, imageUrl: "https://img", imageAssetId: imageAssetId);
        recipe.AddStep(1, "Initial step");

        IRecipeRepository repository = CreateRecipeRepository(recipeId, userId, recipe);
        UpdateRecipeCommandHandler handler = UpdateRecipeHandler(
            repository,
            new NoopImageAssetCleanupService(),
            CreateUserRepository(User.Create("user@example.com", "hash")),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance,
            new AllowAllProductLookupService(),
            new AllowAllRecipeLookupService());

        var command = new UpdateRecipeCommand(
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
            ClearImageUrl: true,
            ImageAssetId: null,
            ClearImageAssetId: true,
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
            Steps: [CreateStep(order: 1, "Initial step")]);

        await handler.Handle(command, CancellationToken.None);

        Assert.Null(recipe.ImageUrl);
        Assert.Null(recipe.ImageAssetId);
    }

    [Fact]
    public async Task Handle_WhenImageAssetAccessFails_ReturnsFailure() {
        var userId = UserId.New();
        var recipeId = RecipeId.New();
        var recipe = Recipe.Create(userId, "Soup", servings: 2);
        recipe.AddStep(1, "Initial step");
        RecordingImageAssetAccessService imageAccess = new FoodDiary.Application.Tests.RecordingImageAssetAccessService()
            .WithFailure(Errors.Image.Forbidden());
        UpdateRecipeCommandHandler handler = UpdateRecipeHandler(
            CreateRecipeRepository(recipeId, userId, recipe),
            new NoopImageAssetCleanupService(),
            CreateUserRepository(User.Create("image-fail@example.com", "hash")),
            imageAccess,
            new AllowAllProductLookupService(),
            new AllowAllRecipeLookupService());

        Result<RecipeModel> result = await handler.Handle(
            UpdateCommand(userId.Value, recipeId.Value, imageAssetId: ImageAssetId.New().Value),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Image.Forbidden", result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenStepImageAssetAccessFails_ReturnsFailure() {
        var userId = UserId.New();
        var recipeId = RecipeId.New();
        var recipe = Recipe.Create(userId, "Soup", servings: 2);
        recipe.AddStep(1, "Initial step");
        UpdateRecipeCommandHandler handler = UpdateRecipeHandler(
            CreateRecipeRepository(recipeId, userId, recipe),
            new NoopImageAssetCleanupService(),
            CreateUserRepository(User.Create("step-image-fail@example.com", "hash")),
            new FailingNonNullImageAssetAccessService(Errors.Image.Forbidden()),
            new AllowAllProductLookupService(),
            new AllowAllRecipeLookupService());

        Result<RecipeModel> result = await handler.Handle(
            UpdateCommand(
                userId.Value,
                recipeId.Value,
                steps: [
                    new RecipeStepInput(
                        Order: 1,
                        Description: "Step",
                        Title: null,
                        ImageUrl: null,
                        ImageAssetId: ImageAssetId.New().Value,
                        Ingredients: []),
                ]),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Image.Forbidden", result.Error.Code);
    }

    [Fact]
    public async Task Handle_WithNewRecipeAndStepAssets_CleansOldUnusedAssets() {
        var userId = UserId.New();
        var recipeId = RecipeId.New();
        var oldRecipeAssetId = ImageAssetId.New();
        var oldStepAssetId = ImageAssetId.New();
        var newRecipeAssetId = ImageAssetId.New();
        var newStepAssetId = ImageAssetId.New();
        var recipe = Recipe.Create(userId, "Soup", servings: 2, imageUrl: "https://old", imageAssetId: oldRecipeAssetId);
        SetRecipeId(recipe, recipeId);
        recipe.AddStep(1, "Initial step", imageUrl: "https://old-step", imageAssetId: oldStepAssetId);
        var cleanup = new RecordingImageAssetCleanupService();
        UpdateRecipeCommandHandler handler = UpdateRecipeHandler(
            CreateRecipeRepository(recipeId, userId, recipe),
            cleanup,
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
                ImageAssetId: newRecipeAssetId.Value,
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
                Steps: [new RecipeStepInput(
                    Order: 1,
                    Description: "Updated step",
                    Title: "Prep",
                    ImageUrl: null,
                    ImageAssetId: newStepAssetId.Value,
                    Ingredients: [new RecipeIngredientInput(ProductId: ProductId.New().Value, NestedRecipeId: null, Amount: 100)])]),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(newRecipeAssetId, recipe.ImageAssetId);
        Assert.Equal([oldRecipeAssetId, oldStepAssetId], cleanup.RequestedAssetIds);
    }
}
