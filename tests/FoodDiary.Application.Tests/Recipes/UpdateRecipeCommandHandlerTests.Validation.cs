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
    public async Task Handle_WithDuplicateStepOrder_ThrowsArgumentException() {
        var userId = UserId.New();
        var recipeId = RecipeId.New();
        var recipe = Recipe.Create(userId, "Soup", servings: 2);
        SetRecipeId(recipe, recipeId);
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
            PrepTime: 10,
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
            Steps: [
                CreateStep(order: 1, "Step 1"),
                CreateStep(order: 1, "Step 2 duplicate order"),
            ]);

        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenManualNutritionMissing_ReturnsValidationFailure() {
        var userId = UserId.New();
        var recipeId = RecipeId.New();
        var recipe = Recipe.Create(userId, "Soup", servings: 2);
        recipe.AddStep(1, "Initial step");

        IRecipeRepository repository = CreateRecipeRepository(recipeId, userId, recipe);
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
                CalculateNutritionAutomatically: false,
                ManualCalories: null,
                ManualProteins: 10,
                ManualFats: 4,
                ManualCarbs: 20,
                ManualFiber: 2,
                ManualAlcohol: 0,
                Steps: [CreateStep(order: 1, "Initial step")]),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Required", result.Error.Code);
        Assert.Contains("calories", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(100.0, null, 4.0, 20.0, 2.0, 0.0, "proteins")]
    [InlineData(100.0, 10.0, null, 20.0, 2.0, 0.0, "fats")]
    [InlineData(100.0, 10.0, 4.0, null, 2.0, 0.0, "carbs")]
    [InlineData(100.0, 10.0, 4.0, 20.0, null, 0.0, "fiber")]
    public async Task Handle_WithManualNutritionRequiredValueMissing_ReturnsValidationFailure(
        double? calories,
        double? proteins,
        double? fats,
        double? carbs,
        double? fiber,
        double? alcohol,
        string expectedField) {
        var userId = UserId.New();
        var recipeId = RecipeId.New();
        var recipe = Recipe.Create(userId, "Soup", servings: 2);
        recipe.AddStep(1, "Initial step");
        UpdateRecipeCommandHandler handler = UpdateRecipeHandler(
            CreateRecipeRepository(recipeId, userId, recipe),
            new NoopImageAssetCleanupService(),
            CreateUserRepository(User.Create("manual-missing@example.com", "hash")),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance,
            new AllowAllProductLookupService(),
            new AllowAllRecipeLookupService());

        Result<RecipeModel> result = await handler.Handle(
            UpdateCommand(
                userId.Value,
                recipeId.Value,
                calculateNutritionAutomatically: false,
                manualCalories: calories,
                manualProteins: proteins,
                manualFats: fats,
                manualCarbs: carbs,
                manualFiber: fiber,
                manualAlcohol: alcohol),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Required", result.Error.Code);
        Assert.Contains(expectedField, result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_WithNegativeManualNutrition_ReturnsValidationFailure() {
        var userId = UserId.New();
        var recipeId = RecipeId.New();
        var recipe = Recipe.Create(userId, "Soup", servings: 2);
        recipe.AddStep(1, "Initial step");
        UpdateRecipeCommandHandler handler = UpdateRecipeHandler(
            CreateRecipeRepository(recipeId, userId, recipe),
            new NoopImageAssetCleanupService(),
            CreateUserRepository(User.Create("manual-negative@example.com", "hash")),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance,
            new AllowAllProductLookupService(),
            new AllowAllRecipeLookupService());

        Result<RecipeModel> result = await handler.Handle(
            UpdateCommand(
                userId.Value,
                recipeId.Value,
                calculateNutritionAutomatically: false,
                manualCalories: 100,
                manualProteins: 10,
                manualFats: 4,
                manualCarbs: 20,
                manualFiber: 2,
                manualAlcohol: -1),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Fact]
    public async Task Handle_WithMissingUserId_ReturnsInvalidToken() {
        var userId = UserId.New();
        var recipeId = RecipeId.New();
        var recipe = Recipe.Create(userId, "Soup", servings: 2);
        recipe.AddStep(1, "Initial step");
        UpdateRecipeCommandHandler handler = UpdateRecipeHandler(
            CreateRecipeRepository(recipeId, userId, recipe),
            new NoopImageAssetCleanupService(),
            CreateUserRepository(User.Create("missing-user@example.com", "hash")),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance,
            new AllowAllProductLookupService(),
            new AllowAllRecipeLookupService());

        Result<RecipeModel> result = await handler.Handle(UpdateCommand(userId: null, recipeId.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenRecipeIsMissing_ReturnsNotAccessible() {
        var userId = UserId.New();
        var recipeId = RecipeId.New();
        var recipe = Recipe.Create(userId, "Soup", servings: 2);
        recipe.AddStep(1, "Initial step");
        UpdateRecipeCommandHandler handler = UpdateRecipeHandler(
            CreateRecipeRepository(recipeId, userId, recipe),
            new NoopImageAssetCleanupService(),
            CreateUserRepository(User.Create("missing-recipe@example.com", "hash")),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance,
            new AllowAllProductLookupService(),
            new AllowAllRecipeLookupService());

        Result<RecipeModel> result = await handler.Handle(UpdateCommand(userId.Value, RecipeId.New().Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Recipe.NotAccessible", result.Error.Code);
    }

    [Fact]
    public async Task Handle_WithInvalidVisibility_ReturnsValidationFailure() {
        var userId = UserId.New();
        var recipeId = RecipeId.New();
        var recipe = Recipe.Create(userId, "Soup", servings: 2);
        recipe.AddStep(1, "Initial step");
        UpdateRecipeCommandHandler handler = UpdateRecipeHandler(
            CreateRecipeRepository(recipeId, userId, recipe),
            new NoopImageAssetCleanupService(),
            CreateUserRepository(User.Create("bad-visibility@example.com", "hash")),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance,
            new AllowAllProductLookupService(),
            new AllowAllRecipeLookupService());

        Result<RecipeModel> result = await handler.Handle(UpdateCommand(userId.Value, recipeId.Value, visibility: "secret"), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Fact]
    public async Task Handle_WithEmptyImageAssetId_ReturnsValidationFailure() {
        var userId = UserId.New();
        var recipeId = RecipeId.New();
        var recipe = Recipe.Create(userId, "Soup", servings: 2);
        recipe.AddStep(1, "Initial step");

        IRecipeRepository repository = CreateRecipeRepository(recipeId, userId, recipe);
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
                Name: "Soup",
                Description: null,
                ClearDescription: false,
                Comment: null,
                ClearComment: false,
                Category: null,
                ClearCategory: false,
                ImageUrl: null,
                ClearImageUrl: false,
                ImageAssetId: Guid.Empty,
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
                Steps: [CreateStep(order: 1, "Initial step")]),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("ImageAssetId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_WithEmptyStepImageAssetId_ReturnsValidationFailure() {
        var userId = UserId.New();
        var recipeId = RecipeId.New();
        var recipe = Recipe.Create(userId, "Soup", servings: 2);
        recipe.AddStep(1, "Initial step");
        UpdateRecipeCommandHandler handler = UpdateRecipeHandler(
            CreateRecipeRepository(recipeId, userId, recipe),
            new NoopImageAssetCleanupService(),
            CreateUserRepository(User.Create("empty-step-image@example.com", "hash")),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance,
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
                        ImageAssetId: Guid.Empty,
                        Ingredients: []),
                ]),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("ImageAssetId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_WithEmptyNestedRecipeId_ReturnsValidationFailure() {
        var userId = UserId.New();
        var recipeId = RecipeId.New();
        var recipe = Recipe.Create(userId, "Soup", servings: 2);
        recipe.AddStep(1, "Initial step");

        IRecipeRepository repository = CreateRecipeRepository(recipeId, userId, recipe);
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
                Steps: [new RecipeStepInput(
                    Order: 1,
                    Description: "Initial step",
                    Title: null,
                    ImageUrl: null,
                    ImageAssetId: null,
                    Ingredients: [new RecipeIngredientInput(ProductId: null, NestedRecipeId: Guid.Empty, Amount: 100)])]),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("NestedRecipeId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_WithEmptyRecipeId_ReturnsValidationFailure() {
        var userId = UserId.New();
        var recipe = Recipe.Create(userId, "Soup", servings: 2);

        UpdateRecipeCommandHandler handler = UpdateRecipeHandler(
            CreateRecipeRepository(RecipeId.New(), userId, recipe),
            new NoopImageAssetCleanupService(),
            CreateUserRepository(User.Create("user@example.com", "hash")),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance,
            new AllowAllProductLookupService(),
            new AllowAllRecipeLookupService());

        Result<RecipeModel> result = await handler.Handle(
            new UpdateRecipeCommand(
                userId.Value,
                Guid.Empty,
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
                Steps: [CreateStep(order: 1, "Initial step")]),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("RecipeId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_WithEmptyIngredientProductId_ReturnsValidationFailure() {
        var userId = UserId.New();
        var recipeId = RecipeId.New();
        var recipe = Recipe.Create(userId, "Soup", servings: 2);
        recipe.AddStep(1, "Initial step");
        UpdateRecipeCommandHandler handler = UpdateRecipeHandler(
            CreateRecipeRepository(recipeId, userId, recipe),
            new NoopImageAssetCleanupService(),
            CreateUserRepository(User.Create("empty-product@example.com", "hash")),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance,
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
                        ImageAssetId: null,
                        Ingredients: [new RecipeIngredientInput(ProductId: Guid.Empty, NestedRecipeId: null, Amount: 1)]),
                ]),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("ProductId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }
}
