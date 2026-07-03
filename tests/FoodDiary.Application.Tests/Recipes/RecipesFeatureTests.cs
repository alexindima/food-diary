using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.FavoriteRecipes.Common;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Application.Abstractions.Products.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Recipes.Commands.CreateRecipe;
using FoodDiary.Application.Recipes.Commands.DeleteRecipe;
using FoodDiary.Application.Recipes.Commands.DuplicateRecipe;
using FoodDiary.Application.Recipes.Commands.UpdateRecipe;
using FoodDiary.Application.Recipes.Common;
using FoodDiary.Application.Recipes.Queries.GetRecipeById;
using FoodDiary.Application.Recipes.Queries.ExploreRecipes;
using FoodDiary.Application.Recipes.Queries.GetRecentRecipes;
using FoodDiary.Application.Recipes.Queries.GetRecipes;
using FoodDiary.Application.Recipes.Queries.GetRecipesOverview;
using FoodDiary.Application.Abstractions.Recipes.Common;
using FoodDiary.Application.Abstractions.RecentItems.Common;
using FoodDiary.Application.Common.Nutrition;
using FoodDiary.Application.Recipes.Services;
using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.Entities.FavoriteRecipes;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FluentValidation.Results;
using FoodDiary.Application.Recipes.Models;
using FoodDiary.Application.Common.Models;

namespace FoodDiary.Application.Tests.Recipes;

[ExcludeFromCodeCoverage]
public class RecipesFeatureTests {
    [Fact]
    public async Task GetRecentRecipesQueryValidator_WithEmptyUserId_Fails() {
        var validator = new GetRecentRecipesQueryValidator();
        var query = new GetRecentRecipesQuery(Guid.Empty, 10, IncludePublic: true);

        ValidationResult result = await validator.ValidateAsync(query);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task GetRecipesOverviewQueryValidator_WithValidUserId_Passes() {
        var validator = new GetRecipesOverviewQueryValidator();
        var query = new GetRecipesOverviewQuery(Guid.NewGuid(), 1, 10, Search: null, IncludePublic: true, 10);

        ValidationResult result = await validator.ValidateAsync(query);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task DeleteRecipeCommandHandler_WhenCleanupFails_StillDeletesRecipeAndReturnsSuccess() {
        var userId = UserId.New();
        var recipeAssetId = ImageAssetId.New();
        var stepAssetId = ImageAssetId.New();
        var recipe = Recipe.Create(
            userId,
            name: "Soup",
            servings: 2,
            imageAssetId: recipeAssetId,
            visibility: Visibility.Private);
        recipe.AddStep(1, "Prepare ingredients", imageAssetId: stepAssetId);

        var repository = new SingleRecipeRepository(recipe);
        var cleanup = new RecordingCleanupService("storage_error");
        var handler = new DeleteRecipeCommandHandler(repository, cleanup);

        Result result = await handler.Handle(new DeleteRecipeCommand(userId.Value, recipe.Id.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.True(repository.DeleteCalled);
        Assert.Equal([recipeAssetId, stepAssetId], cleanup.RequestedAssetIds);
    }

    [Fact]
    public async Task DeleteRecipeCommandHandler_WithEmptyRecipeId_ReturnsValidationFailure() {
        var handler = new DeleteRecipeCommandHandler(
            new SingleRecipeRepository(Recipe.Create(UserId.New(), "Soup", servings: 2)),
            new RecordingCleanupService());

        Result result = await handler.Handle(
            new DeleteRecipeCommand(Guid.NewGuid(), Guid.Empty),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("RecipeId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteRecipeCommandHandler_WithMissingUserId_ReturnsInvalidToken() {
        var repository = new SingleRecipeRepository(Recipe.Create(UserId.New(), "Soup", servings: 2));
        var handler = new DeleteRecipeCommandHandler(repository, new RecordingCleanupService());

        Result result = await handler.Handle(
            new DeleteRecipeCommand(Guid.Empty, RecipeId.New().Value),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
        Assert.False(repository.DeleteCalled);
    }

    [Fact]
    public async Task DeleteRecipeCommandHandler_WhenRecipeIsMissing_ReturnsNotAccessible() {
        var userId = UserId.New();
        var repository = new SingleRecipeRepository(Recipe.Create(userId, "Soup", servings: 2));
        var handler = new DeleteRecipeCommandHandler(repository, new RecordingCleanupService());

        Result result = await handler.Handle(
            new DeleteRecipeCommand(userId.Value, RecipeId.New().Value),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Recipe.NotAccessible", result.Error.Code);
        Assert.False(repository.DeleteCalled);
    }

    [Fact]
    public async Task DeleteRecipeCommandHandler_WhenRecipeIsUsed_ReturnsValidationFailure() {
        var userId = UserId.New();
        var recipe = Recipe.Create(userId, "Used soup", servings: 2);
        SetRecipeUsageCollections(recipe, mealItemsCount: 1, nestedRecipeUsageCount: 0);
        var repository = new SingleRecipeRepository(recipe);
        var handler = new DeleteRecipeCommandHandler(repository, new RecordingCleanupService());

        Result result = await handler.Handle(
            new DeleteRecipeCommand(userId.Value, recipe.Id.Value),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.False(repository.DeleteCalled);
    }

    [Fact]
    public async Task DeleteRecipeCommandValidator_WithEmptyUserId_DoesNotCheckRepositoryAndReturnsInvalidToken() {
        var validator = new DeleteRecipeCommandValidator(
            new SingleRecipeRepository(Recipe.Create(UserId.New(), "Soup", servings: 2)));

        ValidationResult result = await validator.ValidateAsync(new DeleteRecipeCommand(Guid.Empty, RecipeId.New().Value));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => string.Equals(error.ErrorCode, "Authentication.InvalidToken", StringComparison.Ordinal));
    }

    [Fact]
    public async Task DeleteRecipeCommandValidator_WhenRecipeMissing_ReturnsNotFound() {
        var userId = UserId.New();
        var validator = new DeleteRecipeCommandValidator(
            new SingleRecipeRepository(Recipe.Create(userId, "Soup", servings: 2)));

        ValidationResult result = await validator.ValidateAsync(new DeleteRecipeCommand(userId.Value, RecipeId.New().Value));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => string.Equals(error.ErrorCode, "Recipe.NotFound", StringComparison.Ordinal));
    }

    [Fact]
    public async Task DeleteRecipeCommandValidator_WhenRecipeIsUsed_ReturnsValidationFailure() {
        var userId = UserId.New();
        var recipe = Recipe.Create(userId, "Used soup", servings: 2);
        SetRecipeUsageCollections(recipe, mealItemsCount: 1, nestedRecipeUsageCount: 1);
        var validator = new DeleteRecipeCommandValidator(new SingleRecipeRepository(recipe));

        ValidationResult result = await validator.ValidateAsync(new DeleteRecipeCommand(userId.Value, recipe.Id.Value));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => string.Equals(error.ErrorCode, "Validation.Invalid", StringComparison.Ordinal));
    }

    [Fact]
    public async Task DeleteRecipeCommandValidator_WhenRecipeIsUnused_HasNoErrors() {
        var userId = UserId.New();
        var recipe = Recipe.Create(userId, "Unused soup", servings: 2);
        var validator = new DeleteRecipeCommandValidator(new SingleRecipeRepository(recipe));

        ValidationResult result = await validator.ValidateAsync(new DeleteRecipeCommand(userId.Value, recipe.Id.Value));

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task UpdateRecipeCommandValidator_WhenRecipeIsMissing_ReturnsNotFoundError() {
        var validator = new UpdateRecipeCommandValidator(new SingleRecipeRepositoryForCreate());

        ValidationResult result = await validator.ValidateAsync(new UpdateRecipeCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
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
            Steps: [CreateRecipeCreateStep(order: 1, "Mix")]));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => string.Equals(error.ErrorCode, "Recipe.NotFound", StringComparison.Ordinal));
    }

    [Fact]
    public async Task UpdateRecipeCommandValidator_WithConflictingClearFlagsDuplicateStepsAndMissingManualNutrition_ReturnsErrors() {
        var userId = UserId.New();
        var recipe = Recipe.Create(userId, "Soup", servings: 2);
        var validator = new UpdateRecipeCommandValidator(new SingleRecipeRepository(recipe));

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
        var handler = new UpdateRecipeCommandHandler(
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
        var handler = new UpdateRecipeCommandHandler(
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

    [Fact]
    public async Task CreateRecipeCommandHandler_WhenManualNutritionMissing_ReturnsValidationFailure() {
        var userId = UserId.New();
        var repository = new SingleRecipeRepositoryForCreate();
        var handler = new CreateRecipeCommandHandler(repository, new StubUserRepository(User.Create("user@example.com", "hash")), FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance,
            new AllowAllProductLookupService(),
            new AllowAllRecipeLookupService());

        Result<RecipeModel> result = await handler.Handle(
            new CreateRecipeCommand(
                userId.Value,
                Name: "Soup",
                Description: null,
                Comment: null,
                Category: null,
                ImageUrl: null,
                ImageAssetId: null,
                PrepTime: 10,
                CookTime: 20,
                Servings: 2,
                Visibility: Visibility.Private.ToString(),
                CalculateNutritionAutomatically: false,
                ManualCalories: null,
                ManualProteins: 10,
                ManualFats: 4,
                ManualCarbs: 20,
                ManualFiber: 2,
                ManualAlcohol: 0,
                Steps: [CreateRecipeCreateStep(order: 1, "Step 1")]),
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
    public async Task CreateRecipeCommandHandler_WhenManualNutritionRequiredValueIsMissing_ReturnsValidationFailure(
        double? calories,
        double? proteins,
        double? fats,
        double? carbs,
        double? fiber,
        double? alcohol,
        string expectedField) {
        var userId = UserId.New();
        var repository = new SingleRecipeRepositoryForCreate();
        var handler = new CreateRecipeCommandHandler(
            repository,
            new StubUserRepository(User.Create("manual-missing@example.com", "hash")),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance,
            new AllowAllProductLookupService(),
            new AllowAllRecipeLookupService());

        Result<RecipeModel> result = await handler.Handle(
            CreateRecipeCommand(
                userId.Value,
                calculateNutritionAutomatically: false,
                manualCalories: calories,
                manualProteins: proteins,
                manualFats: fats,
                manualCarbs: carbs,
                manualFiber: fiber,
                manualAlcohol: alcohol,
                steps: [CreateRecipeCreateStep(order: 1, "Step 1")]),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Required", result.Error.Code);
        Assert.Contains(expectedField, result.Error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Null(repository.LastAddedRecipe);
    }

    [Fact]
    public async Task CreateRecipeCommandHandler_WhenManualNutritionIsNegative_ReturnsValidationFailure() {
        var userId = UserId.New();
        var repository = new SingleRecipeRepositoryForCreate();
        var handler = new CreateRecipeCommandHandler(
            repository,
            new StubUserRepository(User.Create("manual-negative@example.com", "hash")),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance,
            new AllowAllProductLookupService(),
            new AllowAllRecipeLookupService());

        Result<RecipeModel> result = await handler.Handle(
            CreateRecipeCommand(
                userId.Value,
                calculateNutritionAutomatically: false,
                manualCalories: 100,
                manualProteins: 10,
                manualFats: -1,
                manualCarbs: 20,
                manualFiber: 2,
                manualAlcohol: 0,
                steps: [CreateRecipeCreateStep(order: 1, "Step 1")]),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Null(repository.LastAddedRecipe);
    }

    [Fact]
    public async Task CreateRecipeCommandHandler_WithMissingUserId_ReturnsInvalidToken() {
        var repository = new SingleRecipeRepositoryForCreate();
        var handler = new CreateRecipeCommandHandler(
            repository,
            new StubUserRepository(User.Create("missing-user@example.com", "hash")),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance,
            new AllowAllProductLookupService(),
            new AllowAllRecipeLookupService());

        Result<RecipeModel> result = await handler.Handle(CreateRecipeCommand(userId: null), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
        Assert.Null(repository.LastAddedRecipe);
    }

    [Fact]
    public async Task CreateRecipeCommandHandler_WithInvalidVisibility_ReturnsValidationFailure() {
        var repository = new SingleRecipeRepositoryForCreate();
        var handler = new CreateRecipeCommandHandler(
            repository,
            new StubUserRepository(User.Create("bad-visibility@example.com", "hash")),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance,
            new AllowAllProductLookupService(),
            new AllowAllRecipeLookupService());

        Result<RecipeModel> result = await handler.Handle(CreateRecipeCommand(UserId.New().Value, visibility: "secret"), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Null(repository.LastAddedRecipe);
    }

    [Fact]
    public async Task CreateRecipeCommandHandler_WhenImageAssetAccessFails_ReturnsFailure() {
        RecordingImageAssetAccessService imageAccess = new FoodDiary.Application.Tests.RecordingImageAssetAccessService()
            .WithFailure(Errors.Image.Forbidden());
        var repository = new SingleRecipeRepositoryForCreate();
        var handler = new CreateRecipeCommandHandler(
            repository,
            new StubUserRepository(User.Create("image-fail@example.com", "hash")),
            imageAccess,
            new AllowAllProductLookupService(),
            new AllowAllRecipeLookupService());

        Result<RecipeModel> result = await handler.Handle(
            CreateRecipeCommand(UserId.New().Value, imageAssetId: ImageAssetId.New().Value),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Image.Forbidden", result.Error.Code);
        Assert.Null(repository.LastAddedRecipe);
    }

    [Fact]
    public async Task CreateRecipeCommandHandler_WhenStepImageAssetAccessFails_ReturnsFailure() {
        var stepImageAssetId = ImageAssetId.New();
        var repository = new SingleRecipeRepositoryForCreate();
        var handler = new CreateRecipeCommandHandler(
            repository,
            new StubUserRepository(User.Create("step-image-fail@example.com", "hash")),
            new FailingNonNullImageAssetAccessService(Errors.Image.Forbidden()),
            new AllowAllProductLookupService(),
            new AllowAllRecipeLookupService());

        Result<RecipeModel> result = await handler.Handle(
            CreateRecipeCommand(
                UserId.New().Value,
                steps: [
                    new RecipeStepInput(
                        Order: 1,
                        Description: "Step with image",
                        Title: null,
                        ImageUrl: null,
                        ImageAssetId: stepImageAssetId.Value,
                        Ingredients: []),
                ]),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Image.Forbidden", result.Error.Code);
        Assert.Null(repository.LastAddedRecipe);
    }

    [Fact]
    public async Task CreateRecipeCommandHandler_WithEmptyNestedRecipeId_ReturnsValidationFailure() {
        var repository = new SingleRecipeRepositoryForCreate();
        var handler = new CreateRecipeCommandHandler(
            repository,
            new StubUserRepository(User.Create("empty-nested-recipe@example.com", "hash")),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance,
            new AllowAllProductLookupService(),
            new AllowAllRecipeLookupService());

        Result<RecipeModel> result = await handler.Handle(
            CreateRecipeCommand(
                UserId.New().Value,
                steps: [
                    new RecipeStepInput(
                        Order: 1,
                        Description: "Use nested recipe",
                        Title: null,
                        ImageUrl: null,
                        ImageAssetId: null,
                        Ingredients: [new RecipeIngredientInput(ProductId: null, NestedRecipeId: Guid.Empty, Amount: 1)]),
                ]),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Null(repository.LastAddedRecipe);
    }

    [Fact]
    public async Task CreateRecipeCommandHandler_WithNestedRecipeIngredient_PersistsNestedIngredient() {
        var user = User.Create("nested-create@example.com", "hash");
        var nestedRecipeId = RecipeId.New();
        var repository = new SingleRecipeRepositoryForCreate();
        var handler = new CreateRecipeCommandHandler(
            repository,
            new StubUserRepository(user),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance,
            new AllowAllProductLookupService(),
            new AllowAllRecipeLookupService());

        Result<RecipeModel> result = await handler.Handle(
            CreateRecipeCommand(
                user.Id.Value,
                steps: [
                    new RecipeStepInput(
                        Order: 1,
                        Description: "Use nested recipe",
                        Title: null,
                        ImageUrl: null,
                        ImageAssetId: null,
                        Ingredients: [new RecipeIngredientInput(ProductId: null, NestedRecipeId: nestedRecipeId.Value, Amount: 2)]),
                ]),
            CancellationToken.None);

        ResultAssert.Success(result);
        RecipeIngredient ingredient = Assert.Single(Assert.Single(repository.LastAddedRecipe!.Steps).Ingredients);
        Assert.Equal(nestedRecipeId, ingredient.NestedRecipeId);
    }

    [Fact]
    public async Task CreateRecipeCommandHandler_WithValidCommand_PersistsAndReturnsOwnedModel() {
        var user = User.Create("create-recipe@example.com", "hash");
        var repository = new SingleRecipeRepositoryForCreate();
        var handler = new CreateRecipeCommandHandler(repository, new StubUserRepository(user), FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance,
            new AllowAllProductLookupService(),
            new AllowAllRecipeLookupService());

        Result<RecipeModel> result = await handler.Handle(
            new CreateRecipeCommand(
                user.Id.Value,
                Name: "Tomato Soup",
                Description: "Creamy soup",
                Comment: "Serve warm",
                Category: "Dinner",
                ImageUrl: "https://cdn.test/soup.png",
                ImageAssetId: null,
                PrepTime: 15,
                CookTime: 30,
                Servings: 4,
                Visibility: Visibility.Private.ToString(),
                CalculateNutritionAutomatically: false,
                ManualCalories: 320,
                ManualProteins: 14,
                ManualFats: 9,
                ManualCarbs: 40,
                ManualFiber: 6,
                ManualAlcohol: 0,
                Steps: [
                    CreateRecipeCreateStep(order: 1, "Chop vegetables"),
                    CreateRecipeCreateStep(order: 2, "Boil soup"),
                ]),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.NotNull(repository.LastAddedRecipe);
        Assert.Equal("Tomato Soup", repository.LastAddedRecipe.Name);
        Assert.Equal(2, repository.LastAddedRecipe.Steps.Count);
        Assert.True(result.Value.IsOwnedByCurrentUser);
        Assert.Equal("Serve warm", result.Value.Comment);
        Assert.Equal(2, result.Value.Steps.Count);
    }

    [Fact]
    public async Task CreateRecipeCommandHandler_WithInaccessibleProductIngredient_ReturnsValidationFailure() {
        var user = User.Create("create-recipe-inaccessible-product@example.com", "hash");
        var repository = new SingleRecipeRepositoryForCreate();
        var handler = new CreateRecipeCommandHandler(
            repository,
            new StubUserRepository(user),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance,
            new EmptyProductLookupService(),
            new AllowAllRecipeLookupService());

        var productId = Guid.NewGuid();
        Result<RecipeModel> result = await handler.Handle(
            new CreateRecipeCommand(
                user.Id.Value,
                Name: "Tomato Soup",
                Description: null,
                Comment: null,
                Category: null,
                ImageUrl: null,
                ImageAssetId: null,
                PrepTime: 15,
                CookTime: 30,
                Servings: 4,
                Visibility: Visibility.Private.ToString(),
                CalculateNutritionAutomatically: true,
                ManualCalories: null,
                ManualProteins: null,
                ManualFats: null,
                ManualCarbs: null,
                ManualFiber: null,
                ManualAlcohol: null,
                Steps: [CreateRecipeStepWithProduct(order: 1, "Chop vegetables", productId)]),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("Product", result.Error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Null(repository.LastAddedRecipe);
    }

    [Fact]
    public async Task GetRecipeByIdQueryHandler_WithEmptyRecipeId_ReturnsValidationFailure() {
        var handler = new GetRecipeByIdQueryHandler(new SingleRecipeRepositoryForCreate(), new StubUserRepository(User.Create("user@example.com", "hash")));

        Result<RecipeModel> result = await handler.Handle(
            new GetRecipeByIdQuery(Guid.NewGuid(), Guid.Empty, IncludePublic: false),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("RecipeId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetRecipeByIdQueryHandler_WithMissingUserId_ReturnsInvalidToken() {
        var handler = new GetRecipeByIdQueryHandler(new SingleRecipeRepositoryForCreate(), new StubUserRepository(User.Create("user@example.com", "hash")));

        Result<RecipeModel> result = await handler.Handle(
            new GetRecipeByIdQuery(UserId: null, Guid.NewGuid(), IncludePublic: false),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetRecipeByIdQueryHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-recipe-reader@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var recipe = Recipe.Create(user.Id, "Soup", servings: 1);
        var handler = new GetRecipeByIdQueryHandler(new SingleRecipeRepository(recipe), new StubUserRepository(user));

        Result<RecipeModel> result = await handler.Handle(
            new GetRecipeByIdQuery(user.Id.Value, recipe.Id.Value, IncludePublic: false),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task GetRecipeByIdQueryHandler_WithAccessibleRecipe_ReturnsUsageAndOwnerComment() {
        var user = User.Create("recipe-by-id@example.com", "hash");
        var recipe = Recipe.Create(
            user.Id,
            "Chicken Soup",
            servings: 3,
            description: "Rich broth",
            comment: "Private note",
            category: "Lunch",
            visibility: Visibility.Private);
        recipe.AddStep(1, "Prepare ingredients");
        SetRecipeUsageCollections(recipe, mealItemsCount: 2, nestedRecipeUsageCount: 1);

        var handler = new GetRecipeByIdQueryHandler(new SingleRecipeRepository(recipe), new StubUserRepository(user));

        Result<RecipeModel> result = await handler.Handle(new GetRecipeByIdQuery(user.Id.Value, recipe.Id.Value, IncludePublic: false), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(recipe.Id.Value, result.Value.Id);
        Assert.Equal(3, result.Value.UsageCount);
        Assert.True(result.Value.IsOwnedByCurrentUser);
        Assert.Equal("Private note", result.Value.Comment);
    }

    [Fact]
    public async Task DuplicateRecipeCommandHandler_WithEmptyRecipeId_ReturnsValidationFailure() {
        var handler = new DuplicateRecipeCommandHandler(new SingleRecipeRepositoryForCreate());

        Result<RecipeModel> result = await handler.Handle(
            new DuplicateRecipeCommand(Guid.NewGuid(), Guid.Empty),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("RecipeId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DuplicateRecipeCommandHandler_WithEmptyUserId_ReturnsInvalidToken() {
        var handler = new DuplicateRecipeCommandHandler(new SingleRecipeRepositoryForCreate());

        Result<RecipeModel> result = await handler.Handle(
            new DuplicateRecipeCommand(Guid.Empty, Guid.NewGuid()),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task DuplicateRecipeCommandHandler_WhenOriginalRecipeIsMissing_ReturnsNotFound() {
        var userId = Guid.NewGuid();
        var handler = new DuplicateRecipeCommandHandler(new SingleRecipeRepositoryForCreate());

        Result<RecipeModel> result = await handler.Handle(
            new DuplicateRecipeCommand(userId, Guid.NewGuid()),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Recipe.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task DuplicateRecipeCommandHandler_WithExistingRecipe_CopiesFieldsAndClearsImageAsset() {
        var user = User.Create("duplicate-recipe@example.com", "hash");
        var nestedRecipeId = RecipeId.New();
        var original = Recipe.Create(
            user.Id,
            "Original Soup",
            servings: 2,
            description: "Rich soup",
            comment: "Original note",
            category: "Dinner",
            imageUrl: "https://cdn.test/original-soup.png",
            imageAssetId: ImageAssetId.New(),
            prepTime: 15,
            cookTime: 35,
            visibility: Visibility.Public);
        RecipeStep step = original.AddStep(1, "Boil water", "Prep", "https://cdn.test/step.png", ImageAssetId.New());
        step.AddProductIngredient(ProductId.New(), 200);
        step.AddNestedRecipeIngredient(nestedRecipeId, 50);

        var repository = new SingleRecipeRepository(original);
        var handler = new DuplicateRecipeCommandHandler(repository);

        Result<RecipeModel> result = await handler.Handle(new DuplicateRecipeCommand(user.Id.Value, original.Id.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.NotNull(repository.LastAddedRecipe);
        Assert.NotEqual(original.Id, repository.LastAddedRecipe.Id);
        Assert.Equal(original.Name, repository.LastAddedRecipe.Name);
        Assert.Equal(original.ImageUrl, repository.LastAddedRecipe.ImageUrl);
        Assert.Null(repository.LastAddedRecipe.ImageAssetId);
        Assert.Equal(user.Id, repository.LastAddedRecipe.UserId);
        Assert.Single(repository.LastAddedRecipe.Steps);
        Assert.Equal(2, repository.LastAddedRecipe.Steps.Single().Ingredients.Count);
        Assert.True(result.Value.IsOwnedByCurrentUser);
    }

    [Fact]
    public async Task DuplicateRecipeCommandHandler_WithUnorderedSteps_CopiesStepsInStepNumberOrder() {
        var user = User.Create("duplicate-ordered-steps@example.com", "hash");
        var original = Recipe.Create(user.Id, "Ordered Recipe", servings: 1);
        original.AddStep(3, "Third");
        original.AddStep(1, "First");
        original.AddStep(2, "Second");
        var repository = new SingleRecipeRepository(original);
        var handler = new DuplicateRecipeCommandHandler(repository);

        Result<RecipeModel> result = await handler.Handle(new DuplicateRecipeCommand(user.Id.Value, original.Id.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.NotNull(repository.LastAddedRecipe);
        Assert.Equal([1, 2, 3], [.. repository.LastAddedRecipe.Steps.Select(step => step.StepNumber)]);
    }

    [Fact]
    public async Task DuplicateRecipeCommandHandler_WithManualNutrition_CopiesManualValues() {
        var user = User.Create("duplicate-manual-recipe@example.com", "hash");
        var original = Recipe.Create(user.Id, "Manual Recipe", servings: 1);
        original.SetManualNutrition(250, 20, 8, 30, 5, 1);

        var repository = new SingleRecipeRepository(original);
        var handler = new DuplicateRecipeCommandHandler(repository);

        Result<RecipeModel> result = await handler.Handle(new DuplicateRecipeCommand(user.Id.Value, original.Id.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.NotNull(repository.LastAddedRecipe);
        Assert.False(repository.LastAddedRecipe.IsNutritionAutoCalculated);
        Assert.Equal(250, repository.LastAddedRecipe.ManualCalories);
        Assert.Equal(20, repository.LastAddedRecipe.ManualProteins);
        Assert.Equal(8, repository.LastAddedRecipe.ManualFats);
        Assert.Equal(30, repository.LastAddedRecipe.ManualCarbs);
        Assert.Equal(5, repository.LastAddedRecipe.ManualFiber);
        Assert.Equal(1, repository.LastAddedRecipe.ManualAlcohol);
    }

    [Fact]
    public async Task DuplicateRecipeCommandHandler_ReturnsDuplicatedRecipeWithoutReloadingBeforeCommit() {
        var user = User.Create("duplicate-no-reload@example.com", "hash");
        var original = Recipe.Create(user.Id, "Original Recipe", servings: 1);
        var repository = new SingleRecipeRepository(original);
        var handler = new DuplicateRecipeCommandHandler(repository);

        Result<RecipeModel> result = await handler.Handle(new DuplicateRecipeCommand(user.Id.Value, original.Id.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal("Original Recipe", result.Value.Name);
        Assert.NotNull(repository.LastAddedRecipe);
        Assert.NotEqual(original.Id.Value, result.Value.Id);
    }

    [Fact]
    public async Task GetRecipesOverviewQueryHandler_WithMissingUserId_ReturnsInvalidToken() {
        var handler = new GetRecipesOverviewQueryHandler(
            new OverviewRecipeRepository(),
            new StubRecentItemRepository([]),
            new StubFavoriteRecipeRepository([]),
            new StubUserRepository(User.Create("overview-missing-user@example.com", "hash")));

        Result<RecipeOverviewModel> result = await handler.Handle(
            new GetRecipesOverviewQuery(UserId: null, 1, 10, Search: null, IncludePublic: true, 10, 10),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetRecipesOverviewQueryHandler_WhenUserAccessFails_ReturnsAccessFailure() {
        var user = User.Create("overview-inactive-user@example.com", "hash");
        user.Deactivate();
        var handler = new GetRecipesOverviewQueryHandler(
            new OverviewRecipeRepository(),
            new StubRecentItemRepository([]),
            new StubFavoriteRecipeRepository([]),
            new StubUserRepository(user));

        Result<RecipeOverviewModel> result = await handler.Handle(
            new GetRecipesOverviewQuery(user.Id.Value, 1, 10, Search: null, IncludePublic: true),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetRecipesOverviewQueryHandler_WithoutSearch_ReturnsRecentFavoritesAndFavoriteFlags() {
        var user = User.Create("overview-recipes@example.com", "hash");
        var breakfast = Recipe.Create(
            user.Id,
            "Breakfast Bowl",
            servings: 1,
            category: "Breakfast",
            visibility: Visibility.Private);
        breakfast.AddStep(1, "Mix ingredients");

        var dinner = Recipe.Create(
            user.Id,
            "Dinner Soup",
            servings: 2,
            category: "Dinner",
            visibility: Visibility.Private);
        dinner.AddStep(1, "Cook soup");

        var favorite = FavoriteRecipe.Create(user.Id, dinner.Id, "Fav dinner");
        SetFavoriteRecipeNavigation(favorite, dinner);

        var repository = new OverviewRecipeRepository(
            pagedItems: [(breakfast, 2), (dinner, 5)],
            recipesByIdWithUsage: new Dictionary<RecipeId, (Recipe Recipe, int UsageCount)> {
                [dinner.Id] = (dinner, 5),
            });
        var recentRepository = new StubRecentItemRepository([
            new RecentRecipeUsage(dinner.Id, 5, DateTime.UtcNow),
        ]);
        var favoriteRepository = new StubFavoriteRecipeRepository([favorite]);
        var handler = new GetRecipesOverviewQueryHandler(repository, recentRepository, favoriteRepository, new StubUserRepository(user));

        Result<RecipeOverviewModel> result = await handler.Handle(
            new GetRecipesOverviewQuery(user.Id.Value, 1, 10, Search: null, IncludePublic: true, 10, 10),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(2, result.Value.AllRecipes.Data.Count);
        Assert.Single(result.Value.RecentItems);
        Assert.Single(result.Value.FavoriteItems);
        Assert.Equal(1, result.Value.FavoriteTotalCount);
        Assert.Equal(dinner.Id.Value, result.Value.RecentItems[0].Id);
        Assert.True(result.Value.RecentItems[0].IsFavorite);
        Assert.Equal(favorite.Id.Value, result.Value.RecentItems[0].FavoriteRecipeId);
        Assert.True(result.Value.AllRecipes.Data.Single(x => x.Id == dinner.Id.Value).IsFavorite);
    }

    [Fact]
    public async Task GetRecipesOverviewQueryHandler_WhenThereAreNoRecentRecipes_ReturnsEmptyRecentItems() {
        var user = User.Create("overview-no-recents@example.com", "hash");
        var recipe = Recipe.Create(user.Id, "No Recents Soup", servings: 1);
        recipe.AddStep(1, "Cook");
        var recentRepository = new StubRecentItemRepository([]);
        var handler = new GetRecipesOverviewQueryHandler(
            new OverviewRecipeRepository(pagedItems: [(recipe, 1)]),
            recentRepository,
            new StubFavoriteRecipeRepository([]),
            new StubUserRepository(user));

        Result<RecipeOverviewModel> result = await handler.Handle(
            new GetRecipesOverviewQuery(user.Id.Value, 1, 10, Search: null, IncludePublic: true, 10, 10),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Empty(result.Value.RecentItems);
        Assert.Equal(1, recentRepository.GetRecentRecipesCallCount);
    }

    [Fact]
    public async Task GetRecipesOverviewQueryHandler_WithSearch_SkipsRecentItems() {
        var user = User.Create("overview-search-recipe@example.com", "hash");
        var recipe = Recipe.Create(
            user.Id,
            "Protein Pancakes",
            servings: 2,
            category: "Breakfast",
            visibility: Visibility.Private);
        recipe.AddStep(1, "Cook pancakes");

        var repository = new OverviewRecipeRepository(pagedItems: [(recipe, 1)]);
        var recentRepository = new StubRecentItemRepository([
            new RecentRecipeUsage(recipe.Id, 1, DateTime.UtcNow),
        ]);
        var favoriteRepository = new StubFavoriteRecipeRepository([]);
        var handler = new GetRecipesOverviewQueryHandler(repository, recentRepository, favoriteRepository, new StubUserRepository(user));

        Result<RecipeOverviewModel> result = await handler.Handle(
            new GetRecipesOverviewQuery(user.Id.Value, 1, 10, "protein", IncludePublic: true, 10, 10),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Empty(result.Value.RecentItems);
        Assert.Equal(0, recentRepository.GetRecentRecipesCallCount);
    }

    [Fact]
    public async Task GetRecipesOverviewQueryHandler_WithHasImageFilter_FiltersRecentItems() {
        var user = User.Create("overview-recipe-image-filter@example.com", "hash");
        var withImage = Recipe.Create(user.Id, "Photo Soup", servings: 1, imageUrl: "https://cdn.test/soup.jpg", visibility: Visibility.Private);
        var withoutImage = Recipe.Create(user.Id, "Plain Soup", servings: 1, visibility: Visibility.Private);
        var repository = new OverviewRecipeRepository(
            recipesByIdWithUsage: new Dictionary<RecipeId, (Recipe Recipe, int UsageCount)> {
                [withImage.Id] = (withImage, 3),
                [withoutImage.Id] = (withoutImage, 2),
            });
        var handler = new GetRecipesOverviewQueryHandler(
            repository,
            new StubRecentItemRepository([
                new RecentRecipeUsage(withImage.Id, 3, DateTime.UtcNow),
                new RecentRecipeUsage(withoutImage.Id, 2, DateTime.UtcNow),
            ]),
            new StubFavoriteRecipeRepository([]),
            new StubUserRepository(user));

        Result<RecipeOverviewModel> result = await handler.Handle(
            new GetRecipesOverviewQuery(user.Id.Value, 1, 10, Search: null, IncludePublic: true, HasImage: true),
            CancellationToken.None);

        ResultAssert.Success(result);
        RecipeModel recent = Assert.Single(result.Value.RecentItems);
        Assert.Equal(withImage.Id.Value, recent.Id);
    }

    [Fact]
    public async Task GetRecentRecipesQueryHandler_WithMissingUserId_ReturnsInvalidToken() {
        var handler = new GetRecentRecipesQueryHandler(new StubRecentItemRepository([]), new SingleRecipeRepositoryForCreate());

        Result<IReadOnlyList<RecipeModel>> result = await handler.Handle(new GetRecentRecipesQuery(UserId: null, 10, IncludePublic: true), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetRecentRecipesQueryHandler_WhenNoRecentRecipes_ReturnsEmptyList() {
        var userId = UserId.New();
        var recentRepository = new StubRecentItemRepository([]);
        var handler = new GetRecentRecipesQueryHandler(recentRepository, new SingleRecipeRepositoryForCreate());

        Result<IReadOnlyList<RecipeModel>> result = await handler.Handle(new GetRecentRecipesQuery(userId.Value, 10, IncludePublic: true), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Empty(result.Value);
        Assert.Equal(1, recentRepository.GetRecentRecipesCallCount);
    }

    [Fact]
    public async Task GetRecentRecipesQueryHandler_ReturnsRecipesInRecentOrderAndSkipsMissingItems() {
        var userId = UserId.New();
        var owned = Recipe.Create(
            userId,
            "Owned Soup",
            servings: 2,
            category: "Lunch",
            visibility: Visibility.Private);
        owned.AddStep(1, "Cook soup");
        var publicRecipe = Recipe.Create(
            UserId.New(),
            "Public Pancakes",
            servings: 3,
            category: "Breakfast",
            visibility: Visibility.Public);
        publicRecipe.AddStep(1, "Cook pancakes");
        var missingRecipeId = RecipeId.New();
        var repository = new OverviewRecipeRepository(
            recipesByIdWithUsage: new Dictionary<RecipeId, (Recipe Recipe, int UsageCount)> {
                [owned.Id] = (owned, 5),
                [publicRecipe.Id] = (publicRecipe, 2),
            });
        var recentRepository = new StubRecentItemRepository([
            new RecentRecipeUsage(publicRecipe.Id, 2, DateTime.UtcNow),
            new RecentRecipeUsage(missingRecipeId, 9, DateTime.UtcNow),
            new RecentRecipeUsage(owned.Id, 5, DateTime.UtcNow),
        ]);
        var handler = new GetRecentRecipesQueryHandler(recentRepository, repository);

        Result<IReadOnlyList<RecipeModel>> result = await handler.Handle(new GetRecentRecipesQuery(userId.Value, 99, IncludePublic: true), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(2, result.Value.Count);
        Assert.Equal([publicRecipe.Id.Value, owned.Id.Value], [.. result.Value.Select(x => x.Id)]);
        Assert.False(result.Value[0].IsOwnedByCurrentUser);
        Assert.True(result.Value[1].IsOwnedByCurrentUser);
        Assert.Equal(2, result.Value[0].UsageCount);
        Assert.Equal(5, result.Value[1].UsageCount);
    }

    [Fact]
    public async Task ExploreRecipesQueryHandler_ReturnsPagedPublicRecipesAndOwnerFlags() {
        var user = User.Create("explore-recipes@example.com", "hash");
        var owned = Recipe.Create(user.Id, "Owned Public Soup", servings: 2, visibility: Visibility.Public);
        owned.AddStep(1, "Cook");
        var publicRecipe = Recipe.Create(UserId.New(), "Shared Salad", servings: 1, visibility: Visibility.Public);
        publicRecipe.AddStep(1, "Mix");
        var repository = new OverviewRecipeRepository(pagedItems: [(owned, 3), (publicRecipe, 7)]);
        var handler = new ExploreRecipesQueryHandler(repository);

        Result<PagedResponse<RecipeModel>> result = await handler.Handle(
            new ExploreRecipesQuery(user.Id.Value, Page: 0, Limit: 0, Search: "s", Category: "Lunch", MaxPrepTime: 20, SortBy: "popular"),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(1, result.Value.Page);
        Assert.Equal(1, result.Value.Limit);
        Assert.Equal(2, result.Value.TotalItems);
        Assert.True(result.Value.Data[0].IsOwnedByCurrentUser);
        Assert.False(result.Value.Data[1].IsOwnedByCurrentUser);
        Assert.Equal([owned.Id.Value, publicRecipe.Id.Value], [.. result.Value.Data.Select(x => x.Id)]);
    }

    [Fact]
    public async Task CreateRecipeCommandHandler_WithEmptyImageAssetId_ReturnsValidationFailure() {
        var userId = UserId.New();
        var repository = new SingleRecipeRepositoryForCreate();
        var handler = new CreateRecipeCommandHandler(repository, new StubUserRepository(User.Create("user@example.com", "hash")), FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance,
            new AllowAllProductLookupService(),
            new AllowAllRecipeLookupService());

        Result<RecipeModel> result = await handler.Handle(
            new CreateRecipeCommand(
                userId.Value,
                Name: "Soup",
                Description: null,
                Comment: null,
                Category: null,
                ImageUrl: null,
                ImageAssetId: Guid.Empty,
                PrepTime: 10,
                CookTime: 20,
                Servings: 2,
                Visibility: Visibility.Private.ToString(),
                CalculateNutritionAutomatically: true,
                ManualCalories: null,
                ManualProteins: null,
                ManualFats: null,
                ManualCarbs: null,
                ManualFiber: null,
                ManualAlcohol: null,
                Steps: [CreateRecipeCreateStep(order: 1, "Step 1")]),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("ImageAssetId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateRecipeCommandHandler_WithEmptyStepImageAssetId_ReturnsValidationFailure() {
        var userId = UserId.New();
        var repository = new SingleRecipeRepositoryForCreate();
        var handler = new CreateRecipeCommandHandler(repository, new StubUserRepository(User.Create("user@example.com", "hash")), FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance,
            new AllowAllProductLookupService(),
            new AllowAllRecipeLookupService());

        Result<RecipeModel> result = await handler.Handle(
            new CreateRecipeCommand(
                userId.Value,
                Name: "Soup",
                Description: null,
                Comment: null,
                Category: null,
                ImageUrl: null,
                ImageAssetId: null,
                PrepTime: 10,
                CookTime: 20,
                Servings: 2,
                Visibility: Visibility.Private.ToString(),
                CalculateNutritionAutomatically: true,
                ManualCalories: null,
                ManualProteins: null,
                ManualFats: null,
                ManualCarbs: null,
                ManualFiber: null,
                ManualAlcohol: null,
                Steps: [new RecipeStepInput(
                    Order: 1,
                    Description: "Step 1",
                    Title: null,
                    ImageUrl: null,
                    ImageAssetId: Guid.Empty,
                    Ingredients: [new RecipeIngredientInput(ProductId: Guid.NewGuid(), NestedRecipeId: null, Amount: 100)])]),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("ImageAssetId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateRecipeCommandHandler_WithEmptyIngredientProductId_ReturnsValidationFailure() {
        var userId = UserId.New();
        var repository = new SingleRecipeRepositoryForCreate();
        var handler = new CreateRecipeCommandHandler(repository, new StubUserRepository(User.Create("user@example.com", "hash")), FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance,
            new AllowAllProductLookupService(),
            new AllowAllRecipeLookupService());

        Result<RecipeModel> result = await handler.Handle(
            new CreateRecipeCommand(
                userId.Value,
                Name: "Soup",
                Description: null,
                Comment: null,
                Category: null,
                ImageUrl: null,
                ImageAssetId: null,
                PrepTime: 10,
                CookTime: 20,
                Servings: 2,
                Visibility: Visibility.Private.ToString(),
                CalculateNutritionAutomatically: true,
                ManualCalories: null,
                ManualProteins: null,
                ManualFats: null,
                ManualCarbs: null,
                ManualFiber: null,
                ManualAlcohol: null,
                Steps: [new RecipeStepInput(
                    Order: 1,
                    Description: "Step 1",
                    Title: null,
                    ImageUrl: null,
                    ImageAssetId: null,
                    Ingredients: [new RecipeIngredientInput(ProductId: Guid.Empty, NestedRecipeId: null, Amount: 100)])]),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("ProductId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetRecipesQueryHandler_WithValidQuery_ReturnsPagedRecipeModels() {
        var user = User.Create("recipes-list@example.com", "hash");
        var ownedRecipe = Recipe.Create(user.Id, "Owned soup", servings: 2, comment: "Private note", visibility: Visibility.Private);
        ownedRecipe.SetManualNutrition(200, 10, 5, 20, 2, 0);
        var publicOwnerId = UserId.New();
        var publicRecipe = Recipe.Create(publicOwnerId, "Public salad", servings: 1, visibility: Visibility.Public);
        var repository = new OverviewRecipeRepository([
            (ownedRecipe, 3),
            (publicRecipe, 5),
        ]);
        var handler = new GetRecipesQueryHandler(repository, new StubUserRepository(user));

        Result<PagedResponse<RecipeModel>> result = await handler.Handle(
            new GetRecipesQuery(user.Id.Value, Page: 0, Limit: 0, Search: "s", IncludePublic: true),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(1, result.Value.Page);
        Assert.Equal(1, result.Value.Limit);
        Assert.Equal(2, result.Value.TotalPages);
        Assert.Equal(2, result.Value.TotalItems);
        Assert.Equal(2, result.Value.Data.Count);
        RecipeModel owned = result.Value.Data.Single(recipe => recipe.Id == ownedRecipe.Id.Value);
        Assert.True(owned.IsOwnedByCurrentUser);
        Assert.Equal("Private note", owned.Comment);
        Assert.Equal(3, owned.UsageCount);
        RecipeModel publicModel = result.Value.Data.Single(recipe => recipe.Id == publicRecipe.Id.Value);
        Assert.False(publicModel.IsOwnedByCurrentUser);
        Assert.Null(publicModel.Comment);
        Assert.Equal(5, publicModel.UsageCount);
    }

    [Fact]
    public async Task GetRecipesQueryHandler_WithEmptyUserId_ReturnsInvalidToken() {
        var handler = new GetRecipesQueryHandler(
            new OverviewRecipeRepository(),
            new StubUserRepository(User.Create("unused@example.com", "hash")));

        Result<PagedResponse<RecipeModel>> result = await handler.Handle(new GetRecipesQuery(Guid.Empty, 1, 10, Search: null, IncludePublic: true), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetRecipesQueryHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-recipes-list@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new GetRecipesQueryHandler(new OverviewRecipeRepository(), new StubUserRepository(user));

        Result<PagedResponse<RecipeModel>> result = await handler.Handle(new GetRecipesQuery(user.Id.Value, 1, 10, Search: null, IncludePublic: true), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    private static CreateRecipeCommand CreateRecipeCommand(
        Guid? userId,
        string visibility = "Private",
        Guid? imageAssetId = null,
        bool calculateNutritionAutomatically = true,
        double? manualCalories = null,
        double? manualProteins = null,
        double? manualFats = null,
        double? manualCarbs = null,
        double? manualFiber = null,
        double? manualAlcohol = null,
        IReadOnlyList<RecipeStepInput>? steps = null) {
        return new CreateRecipeCommand(
            userId,
            Name: "Soup",
            Description: null,
            Comment: null,
            Category: null,
            ImageUrl: null,
            ImageAssetId: imageAssetId,
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
            Steps: steps ?? []);
    }

    private static RecipeStepInput CreateRecipeCreateStep(int order, string description) {
        return new RecipeStepInput(
            Order: order,
            Description: description,
            Title: null,
            ImageUrl: null,
            ImageAssetId: null,
            Ingredients: [new RecipeIngredientInput(ProductId: Guid.NewGuid(), NestedRecipeId: null, Amount: 100)]);
    }

    private static RecipeStepInput CreateRecipeStepWithProduct(int order, string description, Guid productId) {
        return new RecipeStepInput(
            Order: order,
            Description: description,
            Title: null,
            ImageUrl: null,
            ImageAssetId: null,
            Ingredients: [new RecipeIngredientInput(ProductId: productId, NestedRecipeId: null, Amount: 100)]);
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
                    : Result.Success<ImageAsset?>(value: null));
    }

    [ExcludeFromCodeCoverage]
    private sealed class SingleRecipeRepository(Recipe recipe) : IRecipeRepository {
        public bool DeleteCalled { get; private set; }
        public Recipe? LastAddedRecipe { get; private set; }

        public Task<Recipe> AddAsync(Recipe recipe, CancellationToken cancellationToken = default) {
            LastAddedRecipe = recipe;
            return Task.FromResult(recipe);
        }

        public Task<(IReadOnlyList<(Recipe Recipe, int UsageCount)> Items, int TotalItems)> GetPagedAsync(
            UserId userId,
            bool includePublic,
            int page,
            int limit,
            RecipeQueryFilters filters,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<Recipe?> GetByIdAsync(
            RecipeId id,
            UserId userId,
            bool includePublic = true,
            bool includeSteps = false,
            bool asTracking = false,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(FindById(id, userId));

        private Recipe? FindById(RecipeId id, UserId userId) {
            if (LastAddedRecipe is not null && LastAddedRecipe.Id == id && LastAddedRecipe.UserId == userId) {
                return LastAddedRecipe;
            }

            return id == recipe.Id && userId == recipe.UserId ? recipe : null;
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

        public Task DeleteAsync(Recipe recipe, CancellationToken cancellationToken = default) {
            DeleteCalled = true;
            return Task.CompletedTask;
        }

        public Task UpdateNutritionAsync(Recipe recipe, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<(IReadOnlyList<(Recipe Recipe, int UsageCount)> Items, int TotalItems)> GetExplorePagedAsync(
            int page, int limit, string? search, string? category, int? maxPrepTime, string sortBy,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    [ExcludeFromCodeCoverage]
    private sealed class SingleRecipeRepositoryForCreate : IRecipeRepository {
        public Recipe? LastAddedRecipe { get; private set; }

        public Task<Recipe> AddAsync(Recipe recipe, CancellationToken cancellationToken = default) {
            LastAddedRecipe = recipe;
            return Task.FromResult(recipe);
        }

        public Task<(IReadOnlyList<(Recipe Recipe, int UsageCount)> Items, int TotalItems)> GetPagedAsync(
            UserId userId,
            bool includePublic,
            int page,
            int limit,
            RecipeQueryFilters filters,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<Recipe?> GetByIdAsync(
            RecipeId id,
            UserId userId,
            bool includePublic = true,
            bool includeSteps = false,
            bool asTracking = false,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Recipe?>(LastAddedRecipe is not null && LastAddedRecipe.Id == id ? LastAddedRecipe : null);

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

        public Task UpdateAsync(Recipe recipe, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task DeleteAsync(Recipe recipe, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task UpdateNutritionAsync(Recipe recipe, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<(IReadOnlyList<(Recipe Recipe, int UsageCount)> Items, int TotalItems)> GetExplorePagedAsync(
            int page, int limit, string? search, string? category, int? maxPrepTime, string sortBy,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    [ExcludeFromCodeCoverage]
    private sealed class OverviewRecipeRepository(
        IReadOnlyList<(Recipe Recipe, int UsageCount)>? pagedItems = null,
        IReadOnlyDictionary<RecipeId, (Recipe Recipe, int UsageCount)>? recipesByIdWithUsage = null) : IRecipeRepository {
        private readonly IReadOnlyList<(Recipe Recipe, int UsageCount)> _pagedItems = pagedItems ?? [];
        private readonly IReadOnlyDictionary<RecipeId, (Recipe Recipe, int UsageCount)> _recipesByIdWithUsage = recipesByIdWithUsage ?? new Dictionary<RecipeId, (Recipe Recipe, int UsageCount)>();

        public Task<Recipe> AddAsync(Recipe recipe, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<(IReadOnlyList<(Recipe Recipe, int UsageCount)> Items, int TotalItems)> GetPagedAsync(
            UserId userId,
            bool includePublic,
            int page,
            int limit,
            RecipeQueryFilters filters,
            CancellationToken cancellationToken = default) =>
            Task.FromResult((_pagedItems, _pagedItems.Count));

        public Task<Recipe?> GetByIdAsync(
            RecipeId id,
            UserId userId,
            bool includePublic = true,
            bool includeSteps = false,
            bool asTracking = false,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Recipe?>(_pagedItems.Select(x => x.Recipe).FirstOrDefault(x => x.Id == id && x.UserId == userId));

        public Task<IReadOnlyDictionary<RecipeId, Recipe>> GetByIdsAsync(
            IEnumerable<RecipeId> ids,
            UserId userId,
            bool includePublic = true,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<RecipeId, Recipe>>(new Dictionary<RecipeId, Recipe>());

        public Task<IReadOnlyDictionary<RecipeId, (Recipe Recipe, int UsageCount)>> GetByIdsWithUsageAsync(
            IEnumerable<RecipeId> ids,
            UserId userId,
            bool includePublic = true,
            CancellationToken cancellationToken = default) {
            var idSet = ids.ToHashSet();
            var filtered = _recipesByIdWithUsage
                .Where(pair => idSet.Contains(pair.Key))
                .ToDictionary();
            return Task.FromResult<IReadOnlyDictionary<RecipeId, (Recipe Recipe, int UsageCount)>>(filtered);
        }

        public Task UpdateAsync(Recipe recipe, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAsync(Recipe recipe, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateNutritionAsync(Recipe recipe, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<(IReadOnlyList<(Recipe Recipe, int UsageCount)> Items, int TotalItems)> GetExplorePagedAsync(
            int page, int limit, string? search, string? category, int? maxPrepTime, string sortBy,
            CancellationToken cancellationToken = default) =>
            Task.FromResult((_pagedItems, _pagedItems.Count));
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubRecentItemRepository(IReadOnlyList<RecentRecipeUsage> recentRecipes) : IRecentItemRepository {
        private readonly IReadOnlyList<RecentRecipeUsage> _recentRecipes = recentRecipes;
        public int GetRecentRecipesCallCount { get; private set; }

        public Task RegisterUsageAsync(
            UserId userId,
            IReadOnlyCollection<ProductId> productIds,
            IReadOnlyCollection<RecipeId> recipeIds,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<RecentProductUsage>> GetRecentProductsAsync(
            UserId userId,
            int limit,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<RecentRecipeUsage>> GetRecentRecipesAsync(
            UserId userId,
            int limit,
            CancellationToken cancellationToken = default) {
            GetRecentRecipesCallCount++;
            return Task.FromResult<IReadOnlyList<RecentRecipeUsage>>(_recentRecipes.Take(limit).ToList());
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubFavoriteRecipeRepository(IReadOnlyList<FavoriteRecipe> favorites) : IFavoriteRecipeRepository {
        private readonly IReadOnlyList<FavoriteRecipe> _favorites = favorites;

        public Task<FavoriteRecipe> AddAsync(FavoriteRecipe favorite, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAsync(FavoriteRecipe favorite, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<FavoriteRecipe?> GetByIdAsync(FavoriteRecipeId id, UserId userId, bool asTracking = false, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<FavoriteRecipe?> GetByRecipeIdAsync(RecipeId recipeId, UserId userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyDictionary<RecipeId, FavoriteRecipe>> GetByRecipeIdsAsync(UserId userId, IReadOnlyCollection<RecipeId> recipeIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<RecipeId, FavoriteRecipe>>(_favorites.Where(f => recipeIds.Contains(f.RecipeId)).ToDictionary(f => f.RecipeId));
        public Task<IReadOnlyList<FavoriteRecipe>> GetAllAsync(UserId userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_favorites);
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingCleanupService(string? errorCode = null) : IImageAssetCleanupService {
        public List<ImageAssetId> RequestedAssetIds { get; } = [];

        public Task<DeleteImageAssetResult> DeleteIfUnusedAsync(ImageAssetId assetId, CancellationToken cancellationToken = default) {
            RequestedAssetIds.Add(assetId);
            return Task.FromResult(errorCode is null
                ? new DeleteImageAssetResult(Deleted: true)
                : new DeleteImageAssetResult(Deleted: false, errorCode));
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
    private sealed class EmptyProductLookupService : IProductLookupService {
        public Task<IReadOnlyDictionary<ProductId, Product>> GetAccessibleByIdsAsync(
            IEnumerable<ProductId> ids,
            UserId userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<ProductId, Product>>(new Dictionary<ProductId, Product>());
    }

    [ExcludeFromCodeCoverage]
    private sealed class AllowAllRecipeLookupService : IRecipeLookupService {
        public Task<IReadOnlyDictionary<RecipeId, Recipe>> GetAccessibleByIdsAsync(
            IEnumerable<RecipeId> ids,
            UserId userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<RecipeId, Recipe>>(
                ids.Distinct().ToDictionary(id => id, id => CreateNestedRecipe(userId, id)));
    }

    [ExcludeFromCodeCoverage]
    private sealed class EmptyRecipeLookupService : IRecipeLookupService {
        public Task<IReadOnlyDictionary<RecipeId, Recipe>> GetAccessibleByIdsAsync(
            IEnumerable<RecipeId> ids,
            UserId userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<RecipeId, Recipe>>(new Dictionary<RecipeId, Recipe>());
    }

    private static Product CreateProduct(UserId userId, ProductId productId) {
        var product = Product.Create(userId, "Ingredient", MeasurementUnit.G, 100, defaultPortionAmount: null, 100, 1, 1, 1, 1, 0);
        typeof(Product)
            .GetProperty(nameof(Product.Id))!
            .SetValue(product, productId);
        return product;
    }

    private static Recipe CreateNestedRecipe(UserId userId, RecipeId recipeId) {
        var recipe = Recipe.Create(userId, "Nested", servings: 1);
        typeof(Recipe)
            .GetProperty(nameof(Recipe.Id))!
            .SetValue(recipe, recipeId);
        return recipe;
    }

    [Fact]
    public async Task CreateRecipeCommandHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-recipe@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var repository = new SingleRecipeRepositoryForCreate();
        var handler = new CreateRecipeCommandHandler(repository, new StubUserRepository(user), FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance,
            new AllowAllProductLookupService(),
            new AllowAllRecipeLookupService());

        Result<RecipeModel> result = await handler.Handle(
            new CreateRecipeCommand(
                user.Id.Value,
                Name: "Soup",
                Description: null,
                Comment: null,
                Category: null,
                ImageUrl: null,
                ImageAssetId: null,
                PrepTime: 10,
                CookTime: 20,
                Servings: 2,
                Visibility: Visibility.Private.ToString(),
                CalculateNutritionAutomatically: true,
                ManualCalories: null,
                ManualProteins: null,
                ManualFats: null,
                ManualCarbs: null,
                ManualFiber: null,
                ManualAlcohol: null,
                Steps: [CreateRecipeCreateStep(order: 1, "Step 1")]),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task RecipeIngredientAccessValidator_WithMissingProduct_ReturnsValidationFailure() {
        Result result = await RecipeIngredientAccessValidator.EnsureIngredientsAccessibleAsync(
            [CreateRecipeStepWithProduct(order: 1, "Mix", Guid.NewGuid())],
            recipeId: null,
            UserId.New(),
            new EmptyProductLookupService(),
            new AllowAllRecipeLookupService(),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("Product", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RecipeIngredientAccessValidator_WithSelfReference_ReturnsValidationFailure() {
        var recipeId = RecipeId.New();
        var step = new RecipeStepInput(
            Order: 1,
            Description: "Mix",
            Title: null,
            ImageUrl: null,
            ImageAssetId: null,
            Ingredients: [new RecipeIngredientInput(ProductId: null, NestedRecipeId: recipeId.Value, Amount: 1)]);

        Result result = await RecipeIngredientAccessValidator.EnsureIngredientsAccessibleAsync(
            [step],
            recipeId,
            UserId.New(),
            new AllowAllProductLookupService(),
            new AllowAllRecipeLookupService(),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("itself", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RecipeIngredientAccessValidator_WithMissingNestedRecipe_ReturnsValidationFailure() {
        var step = new RecipeStepInput(
            Order: 1,
            Description: "Mix",
            Title: null,
            ImageUrl: null,
            ImageAssetId: null,
            Ingredients: [new RecipeIngredientInput(ProductId: null, NestedRecipeId: Guid.NewGuid(), Amount: 1)]);

        Result result = await RecipeIngredientAccessValidator.EnsureIngredientsAccessibleAsync(
            [step],
            recipeId: null,
            UserId.New(),
            new AllowAllProductLookupService(),
            new EmptyRecipeLookupService(),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("Nested recipe", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RecipeNutritionUpdater_WhenManualNutrition_DoesNotUpdateRepository() {
        var recipe = Recipe.Create(UserId.New(), "Manual", servings: 1);
        recipe.SetManualNutrition(100, 10, 1, 2, 3, 0);
        var repository = new RecordingRecipeNutritionRepository();

        await RecipeNutritionUpdater.EnsureNutritionAsync(recipe, repository, CancellationToken.None);

        Assert.Equal(0, repository.UpdateNutritionCallCount);
    }

    [Fact]
    public async Task RecipeNutritionUpdater_WhenAutoNutritionChanged_UpdatesComputedNutrition() {
        var userId = UserId.New();
        var product = Product.Create(
            userId,
            "Ingredient",
            MeasurementUnit.G,
            100,
            defaultPortionAmount: null,
            200,
            10,
            5,
            20,
            4,
            0);
        var recipe = Recipe.Create(userId, "Auto", servings: 1);
        recipe.ApplyComputedNutrition(1, 1, 1, 1, 1, 1);
        RecipeStep step = recipe.AddStep(1, "Mix");
        step.AddProductIngredient(product.Id, 100);
        RecipeIngredient ingredient = Assert.Single(step.Ingredients);
        typeof(RecipeIngredient)
            .GetProperty(nameof(RecipeIngredient.Product))!
            .SetValue(ingredient, product);
        var repository = new RecordingRecipeNutritionRepository();

        await RecipeNutritionUpdater.EnsureNutritionAsync(recipe, repository, CancellationToken.None);

        Assert.Equal(1, repository.UpdateNutritionCallCount);
        Assert.Equal(200, recipe.TotalCalories);
        Assert.Equal(10, recipe.TotalProteins);
    }

    [Fact]
    public async Task RecipeNutritionUpdater_WhenComputedNutritionIsAlreadyClose_DoesNotUpdateRepository() {
        var userId = UserId.New();
        var recipe = Recipe.Create(userId, "Empty Auto", servings: 1);
        recipe.ApplyComputedNutrition(0, 0, 0, 0, 0, 0);
        var repository = new RecordingRecipeNutritionRepository();

        await RecipeNutritionUpdater.EnsureNutritionAsync(recipe, repository, CancellationToken.None);

        Assert.Equal(0, repository.UpdateNutritionCallCount);
    }

    [Theory]
    [InlineData(null, null, true)]
    [InlineData(null, 1d, false)]
    [InlineData(1d, null, false)]
    [InlineData(1d, 1.005d, true)]
    [InlineData(1d, 1.02d, false)]
    public void RecipeNutritionUpdater_AreClose_HandlesNullableAndToleranceBranches(double? left, double? right, bool expected) {
        System.Reflection.MethodInfo method = typeof(RecipeNutritionUpdater).GetMethod(
            "AreClose",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;

        bool actual = (bool)method.Invoke(null, [left, right])!;

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void RecipeManualNutritionValidator_WhenCaloriesExceedLimit_ReturnsFailure() {
        Result<(double Calories, double Proteins, double Fats, double Carbs, double Fiber, double Alcohol)> result = RecipeManualNutritionValidator.Validate(
            ManualNutritionLimits.MaxCalories + 1,
            proteins: 10,
            fats: 5,
            carbs: 20,
            fiber: 3,
            alcohol: 0);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Fact]
    public void RecipeManualNutritionValidator_WhenNutrientExceedsLimit_ReturnsFailure() {
        Result<(double Calories, double Proteins, double Fats, double Carbs, double Fiber, double Alcohol)> result = RecipeManualNutritionValidator.Validate(
            calories: 200,
            ManualNutritionLimits.MaxNutrient + 1,
            fats: 5,
            carbs: 20,
            fiber: 3,
            alcohol: 0);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingRecipeNutritionRepository : IRecipeRepository {
        public int UpdateNutritionCallCount { get; private set; }

        public Task<Recipe> AddAsync(Recipe recipe, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<(IReadOnlyList<(Recipe Recipe, int UsageCount)> Items, int TotalItems)> GetPagedAsync(
            UserId userId,
            bool includePublic,
            int page,
            int limit,
            RecipeQueryFilters filters,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Recipe?> GetByIdAsync(
            RecipeId id,
            UserId userId,
            bool includePublic = true,
            bool includeSteps = false,
            bool asTracking = false,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyDictionary<RecipeId, Recipe>> GetByIdsAsync(
            IEnumerable<RecipeId> ids,
            UserId userId,
            bool includePublic = true,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyDictionary<RecipeId, (Recipe Recipe, int UsageCount)>> GetByIdsWithUsageAsync(
            IEnumerable<RecipeId> ids,
            UserId userId,
            bool includePublic = true,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task UpdateAsync(Recipe recipe, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task DeleteAsync(Recipe recipe, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task UpdateNutritionAsync(Recipe recipe, CancellationToken cancellationToken = default) {
            UpdateNutritionCallCount++;
            return Task.CompletedTask;
        }

        public Task<(IReadOnlyList<(Recipe Recipe, int UsageCount)> Items, int TotalItems)> GetExplorePagedAsync(
            int page,
            int limit,
            string? search,
            string? category,
            int? maxPrepTime,
            string sortBy,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubUserRepository(User user) : ICurrentUserAccessService {
        public Task<Error?> EnsureCanAccessAsync(UserId userId, CancellationToken cancellationToken = default) {
            Error? error = user switch {
                { DeletedAt: not null } => Errors.Authentication.AccountDeleted,
                { IsActive: false } => Errors.Authentication.InvalidToken,
                _ => null,
            };
            return Task.FromResult(error);
        }
    }

    private static void SetFavoriteRecipeNavigation(FavoriteRecipe favorite, Recipe recipe) {
        typeof(FavoriteRecipe)
            .GetProperty(nameof(FavoriteRecipe.Recipe))!
            .SetValue(favorite, recipe);
    }

    private static void SetRecipeUsageCollections(Recipe recipe, int mealItemsCount, int nestedRecipeUsageCount) {
        var mealItems = Enumerable.Range(0, mealItemsCount)
            .Select(_ => (FoodDiary.Domain.Entities.Meals.MealItem)null!)
            .ToList();
        var nestedRecipeUsages = Enumerable.Range(0, nestedRecipeUsageCount)
            .Select(_ => (RecipeIngredient)null!)
            .ToList();

        typeof(Recipe)
            .GetField("_mealItems", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .SetValue(recipe, mealItems);
        typeof(Recipe)
            .GetField("_nestedRecipeUsages", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .SetValue(recipe, nestedRecipeUsages);
    }
}
