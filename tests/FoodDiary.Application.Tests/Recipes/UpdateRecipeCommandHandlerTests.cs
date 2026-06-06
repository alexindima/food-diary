using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Application.Abstractions.Products.Common;
using FoodDiary.Application.Abstractions.Recipes.Common;
using FoodDiary.Application.Recipes.Commands.UpdateRecipe;
using FoodDiary.Application.Recipes.Common;
using FoodDiary.Application.Recipes.Models;
using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Recipes;

[ExcludeFromCodeCoverage]
public class UpdateRecipeCommandHandlerTests {
    [Fact]
    public async Task Handle_WithDuplicateStepOrder_ThrowsArgumentException() {
        var userId = UserId.New();
        var recipeId = RecipeId.New();
        var recipe = Recipe.Create(userId, "Soup", servings: 2);
        SetRecipeId(recipe, recipeId);
        recipe.AddStep(1, "Initial step");

        var repository = new StubRecipeRepository(recipeId, userId, recipe);
        var handler = new UpdateRecipeCommandHandler(
            repository,
            new NoopImageAssetCleanupService(),
            new StubUserRepository(User.Create("user@example.com", "hash")),
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
    public async Task Handle_WithClearImageFlags_ClearsRecipeMedia() {
        var userId = UserId.New();
        var recipeId = RecipeId.New();
        var imageAssetId = ImageAssetId.New();
        var recipe = Recipe.Create(userId, "Soup", servings: 2, imageUrl: "https://img", imageAssetId: imageAssetId);
        recipe.AddStep(1, "Initial step");

        var repository = new StubRecipeRepository(recipeId, userId, recipe);
        var handler = new UpdateRecipeCommandHandler(
            repository,
            new NoopImageAssetCleanupService(),
            new StubUserRepository(User.Create("user@example.com", "hash")),
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
    public async Task Handle_WhenManualNutritionMissing_ReturnsValidationFailure() {
        var userId = UserId.New();
        var recipeId = RecipeId.New();
        var recipe = Recipe.Create(userId, "Soup", servings: 2);
        recipe.AddStep(1, "Initial step");

        var repository = new StubRecipeRepository(recipeId, userId, recipe);
        var handler = new UpdateRecipeCommandHandler(
            repository,
            new NoopImageAssetCleanupService(),
            new StubUserRepository(User.Create("user@example.com", "hash")),
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

        Assert.True(result.IsFailure);
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
        var handler = new UpdateRecipeCommandHandler(
            new StubRecipeRepository(recipeId, userId, recipe),
            new NoopImageAssetCleanupService(),
            new StubUserRepository(User.Create("manual-missing@example.com", "hash")),
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

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Required", result.Error.Code);
        Assert.Contains(expectedField, result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_WithNegativeManualNutrition_ReturnsValidationFailure() {
        var userId = UserId.New();
        var recipeId = RecipeId.New();
        var recipe = Recipe.Create(userId, "Soup", servings: 2);
        recipe.AddStep(1, "Initial step");
        var handler = new UpdateRecipeCommandHandler(
            new StubRecipeRepository(recipeId, userId, recipe),
            new NoopImageAssetCleanupService(),
            new StubUserRepository(User.Create("manual-negative@example.com", "hash")),
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

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Fact]
    public async Task Handle_WithMissingUserId_ReturnsInvalidToken() {
        var userId = UserId.New();
        var recipeId = RecipeId.New();
        var recipe = Recipe.Create(userId, "Soup", servings: 2);
        recipe.AddStep(1, "Initial step");
        var handler = new UpdateRecipeCommandHandler(
            new StubRecipeRepository(recipeId, userId, recipe),
            new NoopImageAssetCleanupService(),
            new StubUserRepository(User.Create("missing-user@example.com", "hash")),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance,
            new AllowAllProductLookupService(),
            new AllowAllRecipeLookupService());

        Result<RecipeModel> result = await handler.Handle(UpdateCommand(null, recipeId.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenRecipeIsMissing_ReturnsNotAccessible() {
        var userId = UserId.New();
        var recipeId = RecipeId.New();
        var recipe = Recipe.Create(userId, "Soup", servings: 2);
        recipe.AddStep(1, "Initial step");
        var handler = new UpdateRecipeCommandHandler(
            new StubRecipeRepository(recipeId, userId, recipe),
            new NoopImageAssetCleanupService(),
            new StubUserRepository(User.Create("missing-recipe@example.com", "hash")),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance,
            new AllowAllProductLookupService(),
            new AllowAllRecipeLookupService());

        Result<RecipeModel> result = await handler.Handle(UpdateCommand(userId.Value, RecipeId.New().Value), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Recipe.NotAccessible", result.Error.Code);
    }

    [Fact]
    public async Task Handle_WithInvalidVisibility_ReturnsValidationFailure() {
        var userId = UserId.New();
        var recipeId = RecipeId.New();
        var recipe = Recipe.Create(userId, "Soup", servings: 2);
        recipe.AddStep(1, "Initial step");
        var handler = new UpdateRecipeCommandHandler(
            new StubRecipeRepository(recipeId, userId, recipe),
            new NoopImageAssetCleanupService(),
            new StubUserRepository(User.Create("bad-visibility@example.com", "hash")),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance,
            new AllowAllProductLookupService(),
            new AllowAllRecipeLookupService());

        Result<RecipeModel> result = await handler.Handle(UpdateCommand(userId.Value, recipeId.Value, visibility: "secret"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Fact]
    public async Task Handle_WithBlankVisibility_DoesNotChangeRecipeVisibility() {
        var userId = UserId.New();
        var recipeId = RecipeId.New();
        var recipe = Recipe.Create(userId, "Soup", servings: 2, visibility: Visibility.Private);
        SetRecipeId(recipe, recipeId);
        recipe.AddStep(1, "Initial step");
        var handler = new UpdateRecipeCommandHandler(
            new StubRecipeRepository(recipeId, userId, recipe),
            new NoopImageAssetCleanupService(),
            new StubUserRepository(User.Create("blank-visibility@example.com", "hash")),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance,
            new AllowAllProductLookupService(),
            new AllowAllRecipeLookupService());

        Result<RecipeModel> result = await handler.Handle(UpdateCommand(userId.Value, recipeId.Value, visibility: " "), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(Visibility.Private, recipe.Visibility);
    }

    [Fact]
    public async Task Handle_WhenImageAssetAccessFails_ReturnsFailure() {
        var userId = UserId.New();
        var recipeId = RecipeId.New();
        var recipe = Recipe.Create(userId, "Soup", servings: 2);
        recipe.AddStep(1, "Initial step");
        RecordingImageAssetAccessService imageAccess = new FoodDiary.Application.Tests.RecordingImageAssetAccessService()
            .WithFailure(Errors.Image.Forbidden());
        var handler = new UpdateRecipeCommandHandler(
            new StubRecipeRepository(recipeId, userId, recipe),
            new NoopImageAssetCleanupService(),
            new StubUserRepository(User.Create("image-fail@example.com", "hash")),
            imageAccess,
            new AllowAllProductLookupService(),
            new AllowAllRecipeLookupService());

        Result<RecipeModel> result = await handler.Handle(
            UpdateCommand(userId.Value, recipeId.Value, imageAssetId: ImageAssetId.New().Value),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Image.Forbidden", result.Error.Code);
    }

    [Fact]
    public async Task Handle_WithEmptyImageAssetId_ReturnsValidationFailure() {
        var userId = UserId.New();
        var recipeId = RecipeId.New();
        var recipe = Recipe.Create(userId, "Soup", servings: 2);
        recipe.AddStep(1, "Initial step");

        var repository = new StubRecipeRepository(recipeId, userId, recipe);
        var handler = new UpdateRecipeCommandHandler(
            repository,
            new NoopImageAssetCleanupService(),
            new StubUserRepository(User.Create("user@example.com", "hash")),
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

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("ImageAssetId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_WithEmptyStepImageAssetId_ReturnsValidationFailure() {
        var userId = UserId.New();
        var recipeId = RecipeId.New();
        var recipe = Recipe.Create(userId, "Soup", servings: 2);
        recipe.AddStep(1, "Initial step");
        var handler = new UpdateRecipeCommandHandler(
            new StubRecipeRepository(recipeId, userId, recipe),
            new NoopImageAssetCleanupService(),
            new StubUserRepository(User.Create("empty-step-image@example.com", "hash")),
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

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("ImageAssetId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_WhenStepImageAssetAccessFails_ReturnsFailure() {
        var userId = UserId.New();
        var recipeId = RecipeId.New();
        var recipe = Recipe.Create(userId, "Soup", servings: 2);
        recipe.AddStep(1, "Initial step");
        var handler = new UpdateRecipeCommandHandler(
            new StubRecipeRepository(recipeId, userId, recipe),
            new NoopImageAssetCleanupService(),
            new StubUserRepository(User.Create("step-image-fail@example.com", "hash")),
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

        Assert.True(result.IsFailure);
        Assert.Equal("Image.Forbidden", result.Error.Code);
    }

    [Fact]
    public async Task Handle_WithEmptyNestedRecipeId_ReturnsValidationFailure() {
        var userId = UserId.New();
        var recipeId = RecipeId.New();
        var recipe = Recipe.Create(userId, "Soup", servings: 2);
        recipe.AddStep(1, "Initial step");

        var repository = new StubRecipeRepository(recipeId, userId, recipe);
        var handler = new UpdateRecipeCommandHandler(
            repository,
            new NoopImageAssetCleanupService(),
            new StubUserRepository(User.Create("user@example.com", "hash")),
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

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("NestedRecipeId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_WithEmptyRecipeId_ReturnsValidationFailure() {
        var userId = UserId.New();
        var recipe = Recipe.Create(userId, "Soup", servings: 2);

        var handler = new UpdateRecipeCommandHandler(
            new StubRecipeRepository(RecipeId.New(), userId, recipe),
            new NoopImageAssetCleanupService(),
            new StubUserRepository(User.Create("user@example.com", "hash")),
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

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("RecipeId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_WithSelfNestedRecipeIngredient_ReturnsValidationFailure() {
        var userId = UserId.New();
        var recipeId = RecipeId.New();
        var recipe = Recipe.Create(userId, "Soup", servings: 2);
        recipe.AddStep(1, "Initial step");

        var handler = new UpdateRecipeCommandHandler(
            new StubRecipeRepository(recipeId, userId, recipe),
            new NoopImageAssetCleanupService(),
            new StubUserRepository(User.Create("user@example.com", "hash")),
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

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("itself", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_WithEmptyIngredientProductId_ReturnsValidationFailure() {
        var userId = UserId.New();
        var recipeId = RecipeId.New();
        var recipe = Recipe.Create(userId, "Soup", servings: 2);
        recipe.AddStep(1, "Initial step");
        var handler = new UpdateRecipeCommandHandler(
            new StubRecipeRepository(recipeId, userId, recipe),
            new NoopImageAssetCleanupService(),
            new StubUserRepository(User.Create("empty-product@example.com", "hash")),
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

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("ProductId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_WithNestedRecipeIngredient_AddsNestedRecipeIngredient() {
        var userId = UserId.New();
        var recipeId = RecipeId.New();
        var nestedRecipeId = RecipeId.New();
        var recipe = Recipe.Create(userId, "Soup", servings: 2);
        SetRecipeId(recipe, recipeId);
        recipe.AddStep(1, "Initial step");
        var handler = new UpdateRecipeCommandHandler(
            new StubRecipeRepository(recipeId, userId, recipe),
            new NoopImageAssetCleanupService(),
            new StubUserRepository(User.Create("nested-update@example.com", "hash")),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance,
            new AllowAllProductLookupService(),
            new AllowAllRecipeLookupService());

        Result<RecipeModel> result = await handler.Handle(
            UpdateCommand(
                userId.Value,
                recipeId.Value,
                steps: [CreateStepWithNestedRecipe(order: 1, "Updated step", nestedRecipeId.Value)]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        RecipeIngredient ingredient = Assert.Single(Assert.Single(recipe.Steps).Ingredients);
        Assert.Equal(nestedRecipeId, ingredient.NestedRecipeId);
    }

    [Fact]
    public async Task Handle_WhenPrepTimeOmitted_PreservesExistingPrepTime() {
        var userId = UserId.New();
        var recipeId = RecipeId.New();
        var recipe = Recipe.Create(userId, "Soup", servings: 2, prepTime: 15);
        SetRecipeId(recipe, recipeId);
        recipe.AddStep(1, "Initial step");

        var handler = new UpdateRecipeCommandHandler(
            new StubRecipeRepository(recipeId, userId, recipe),
            new NoopImageAssetCleanupService(),
            new StubUserRepository(User.Create("user@example.com", "hash")),
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

        Assert.True(result.IsSuccess);
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
        var handler = new UpdateRecipeCommandHandler(
            repository,
            new NoopImageAssetCleanupService(),
            new StubUserRepository(User.Create("user@example.com", "hash")),
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

        Assert.True(result.IsFailure);
        Assert.Equal("Recipe.InvalidData", result.Error.Code);
        Assert.True(repository.UpdateCalled);
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
        var handler = new UpdateRecipeCommandHandler(
            new StubRecipeRepository(recipeId, userId, recipe),
            cleanup,
            new StubUserRepository(User.Create("user@example.com", "hash")),
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

        Assert.True(result.IsSuccess);
        Assert.Equal(newRecipeAssetId, recipe.ImageAssetId);
        Assert.Equal([oldRecipeAssetId, oldStepAssetId], cleanup.RequestedAssetIds);
    }

    private static RecipeStepInput CreateStep(int order, string description) {
        return new RecipeStepInput(
            Order: order,
            Description: description,
            Title: null,
            ImageUrl: null,
            ImageAssetId: null,
            Ingredients: [new RecipeIngredientInput(ProductId: Guid.NewGuid(), NestedRecipeId: null, Amount: 100)]);
    }

    private static RecipeStepInput CreateStepWithNestedRecipe(int order, string description, Guid nestedRecipeId) {
        return new RecipeStepInput(
            Order: order,
            Description: description,
            Title: null,
            ImageUrl: null,
            ImageAssetId: null,
            Ingredients: [new RecipeIngredientInput(ProductId: null, NestedRecipeId: nestedRecipeId, Amount: 1)]);
    }

    private static UpdateRecipeCommand UpdateCommand(
        Guid? userId,
        Guid recipeId,
        string? visibility = "Public",
        Guid? imageAssetId = null,
        bool calculateNutritionAutomatically = true,
        double? manualCalories = null,
        double? manualProteins = null,
        double? manualFats = null,
        double? manualCarbs = null,
        double? manualFiber = null,
        double? manualAlcohol = null,
        IReadOnlyList<RecipeStepInput>? steps = null) {
        return new UpdateRecipeCommand(
            userId,
            recipeId,
            Name: "Soup",
            Description: null,
            ClearDescription: false,
            Comment: null,
            ClearComment: false,
            Category: null,
            ClearCategory: false,
            ImageUrl: null,
            ClearImageUrl: false,
            ImageAssetId: imageAssetId,
            ClearImageAssetId: false,
            PrepTime: 10,
            CookTime: 20,
            Servings: 2,
            Visibility: visibility,
            CalculateNutritionAutomatically: calculateNutritionAutomatically,
            ManualCalories: manualCalories,
            ManualProteins: manualProteins,
            ManualFats: manualFats,
            ManualCarbs: manualCarbs,
            ManualFiber: manualFiber,
            ManualAlcohol: manualAlcohol,
            Steps: steps ?? [CreateStep(order: 1, "Initial step")]);
    }

    private static void SetRecipeId(Recipe recipe, RecipeId recipeId) {
        typeof(Recipe)
            .GetProperty(nameof(Recipe.Id))!
            .SetValue(recipe, recipeId);
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubRecipeRepository(RecipeId recipeId, UserId userId, Recipe recipe) : IRecipeRepository {
        private readonly RecipeId _recipeId = recipeId;
        private readonly UserId _userId = userId;
        private readonly Recipe _recipe = recipe;

        public Task<Recipe> AddAsync(Recipe recipe, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<(IReadOnlyList<(Recipe Recipe, int UsageCount)> Items, int TotalItems)> GetPagedAsync(
            UserId userId,
            bool includePublic,
            int page,
            int limit,
            string? search,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<Recipe?> GetByIdAsync(
            RecipeId id,
            UserId userId,
            bool includePublic = true,
            bool includeSteps = false,
            bool asTracking = false,
            CancellationToken cancellationToken = default) {
            if (id == _recipeId && userId == _userId) {
                return Task.FromResult<Recipe?>(_recipe);
            }

            return Task.FromResult<Recipe?>(null);
        }

        public Task<IReadOnlyDictionary<RecipeId, Recipe>> GetByIdsAsync(
            IEnumerable<RecipeId> ids,
            UserId userId,
            bool includePublic = true,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IReadOnlyDictionary<RecipeId, (Recipe Recipe, int UsageCount)>> GetByIdsWithUsageAsync(
            IEnumerable<RecipeId> ids,
            UserId userId,
            bool includePublic = true,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task UpdateAsync(Recipe recipe, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task DeleteAsync(Recipe recipe, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task UpdateNutritionAsync(Recipe recipe, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<(IReadOnlyList<(Recipe Recipe, int UsageCount)> Items, int TotalItems)> GetExplorePagedAsync(
            int page, int limit, string? search, string? category, int? maxPrepTime, string sortBy,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    [ExcludeFromCodeCoverage]
    private sealed class ReloadMissingRecipeRepository(RecipeId recipeId, UserId userId, Recipe recipe) : IRecipeRepository {
        public bool UpdateCalled { get; private set; }

        public Task<Recipe> AddAsync(Recipe addedRecipe, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<(IReadOnlyList<(Recipe Recipe, int UsageCount)> Items, int TotalItems)> GetPagedAsync(
            UserId ownerId,
            bool includePublic,
            int page,
            int limit,
            string? search,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<Recipe?> GetByIdAsync(
            RecipeId id,
            UserId ownerId,
            bool includePublic = true,
            bool includeSteps = false,
            bool asTracking = false,
            CancellationToken cancellationToken = default) {
            if (!UpdateCalled && id == recipeId && ownerId == userId) {
                return Task.FromResult<Recipe?>(recipe);
            }

            return Task.FromResult<Recipe?>(null);
        }

        public Task<IReadOnlyDictionary<RecipeId, Recipe>> GetByIdsAsync(
            IEnumerable<RecipeId> ids,
            UserId ownerId,
            bool includePublic = true,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IReadOnlyDictionary<RecipeId, (Recipe Recipe, int UsageCount)>> GetByIdsWithUsageAsync(
            IEnumerable<RecipeId> ids,
            UserId ownerId,
            bool includePublic = true,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task UpdateAsync(Recipe updatedRecipe, CancellationToken cancellationToken = default) {
            UpdateCalled = true;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Recipe deletedRecipe, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task UpdateNutritionAsync(Recipe updatedRecipe, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<(IReadOnlyList<(Recipe Recipe, int UsageCount)> Items, int TotalItems)> GetExplorePagedAsync(
            int page,
            int limit,
            string? search,
            string? category,
            int? maxPrepTime,
            string sortBy,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    [ExcludeFromCodeCoverage]
    private sealed class FailingNonNullImageAssetAccessService(Error error) : IImageAssetAccessService {
        public Task<Result<ImageAsset?>> ResolveOptionalAsync(
            ImageAssetId? assetId,
            UserId userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                assetId.HasValue
                    ? Result.Failure<ImageAsset?>(error)
                    : Result.Success<ImageAsset?>(null));
    }

    [ExcludeFromCodeCoverage]
    private sealed class NoopImageAssetCleanupService : IImageAssetCleanupService {
        public Task<DeleteImageAssetResult> DeleteIfUnusedAsync(ImageAssetId assetId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new DeleteImageAssetResult(true));

        public Task<int> CleanupOrphansAsync(DateTime olderThanUtc, int batchSize, CancellationToken cancellationToken = default) =>
            Task.FromResult(0);
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingImageAssetCleanupService : IImageAssetCleanupService {
        public List<ImageAssetId> RequestedAssetIds { get; } = [];

        public Task<DeleteImageAssetResult> DeleteIfUnusedAsync(ImageAssetId assetId, CancellationToken cancellationToken = default) {
            RequestedAssetIds.Add(assetId);
            return Task.FromResult(new DeleteImageAssetResult(true));
        }

        public Task<int> CleanupOrphansAsync(DateTime olderThanUtc, int batchSize, CancellationToken cancellationToken = default) =>
            Task.FromResult(0);
    }

    [ExcludeFromCodeCoverage]
    private sealed class AllowAllProductLookupService : IProductLookupService {
        public Task<IReadOnlyDictionary<ProductId, Product>> GetAccessibleByIdsAsync(
            IEnumerable<ProductId> ids,
            UserId userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<ProductId, Product>>(
                ids.Distinct().ToDictionary(id => id, id => CreateProduct(userId, id)));
    }

    [ExcludeFromCodeCoverage]
    private sealed class AllowAllRecipeLookupService : IRecipeLookupService {
        public Task<IReadOnlyDictionary<RecipeId, Recipe>> GetAccessibleByIdsAsync(
            IEnumerable<RecipeId> ids,
            UserId userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<RecipeId, Recipe>>(
                ids.Distinct().ToDictionary(id => id, id => CreateRecipe(userId, id)));
    }

    private static Product CreateProduct(UserId userId, ProductId productId) {
        var product = Product.Create(userId, "Ingredient", MeasurementUnit.G, 100, null, 100, 1, 1, 1, 1, 0);
        typeof(Product)
            .GetProperty(nameof(Product.Id))!
            .SetValue(product, productId);
        return product;
    }

    private static Recipe CreateRecipe(UserId userId, RecipeId recipeId) {
        var recipe = Recipe.Create(userId, "Nested", servings: 1);
        typeof(Recipe)
            .GetProperty(nameof(Recipe.Id))!
            .SetValue(recipe, recipeId);
        return recipe;
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubUserRepository(User user) : IUserRepository {
        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByEmailIncludingDeletedAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default) => Task.FromResult<User?>(user);
        public Task<User?> GetByIdIncludingDeletedAsync(UserId id, CancellationToken cancellationToken = default) => Task.FromResult<User?>(user);
        public Task<User?> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByTelegramUserIdIncludingDeletedAsync(long telegramUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<User> Items, int TotalItems)> GetPagedAsync(string? search, int page, int limit, bool includeDeleted, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<User> RecentUsers)> GetAdminDashboardSummaryAsync(int recentLimit, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Role>> GetRolesByNamesAsync(IReadOnlyList<string> names, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User> AddAsync(User addedUser, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAsync(User updatedUser, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
