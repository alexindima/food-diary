using FoodDiary.Results;
using FoodDiary.Application.Recipes.Commands.DeleteRecipe;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FluentValidation.Results;

namespace FoodDiary.Application.Tests.Recipes;

public partial class RecipesFeatureTests {

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
        DeleteRecipeCommandHandler handler = DeleteRecipeHandler(repository, cleanup);

        Result result = await handler.Handle(new DeleteRecipeCommand(userId.Value, recipe.Id.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.True(repository.DeleteCalled);
        Assert.Equal([recipeAssetId, stepAssetId], cleanup.RequestedAssetIds);
    }


    [Fact]
    public async Task DeleteRecipeCommandHandler_WithEmptyRecipeId_ReturnsValidationFailure() {
        DeleteRecipeCommandHandler handler = DeleteRecipeHandler(
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
        DeleteRecipeCommandHandler handler = DeleteRecipeHandler(repository, new RecordingCleanupService());

        Result result = await handler.Handle(
            new DeleteRecipeCommand(Guid.Empty, RecipeId.New().Value),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
        Assert.False(repository.DeleteCalled);
    }


    [Fact]
    public async Task DeleteRecipeCommandHandler_WhenRecipeIsMissing_ReturnsNotAccessible() {
        var user = User.Create("delete-recipe-missing@example.com", "hash");
        var repository = new SingleRecipeRepository(Recipe.Create(user.Id, "Soup", servings: 2));
        DeleteRecipeCommandHandler handler = DeleteRecipeHandler(repository, new RecordingCleanupService(), new StubUserRepository(user));

        Result result = await handler.Handle(
            new DeleteRecipeCommand(user.Id.Value, RecipeId.New().Value),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Recipe.NotAccessible", result.Error.Code);
        Assert.False(repository.DeleteCalled);
    }


    [Fact]
    public async Task DeleteRecipeCommandHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-delete-recipe@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var recipe = Recipe.Create(user.Id, "Soup", servings: 2);
        var repository = new SingleRecipeRepository(recipe);
        DeleteRecipeCommandHandler handler = DeleteRecipeHandler(
            repository,
            new RecordingCleanupService(),
            new StubUserRepository(user));

        Result result = await handler.Handle(
            new DeleteRecipeCommand(user.Id.Value, recipe.Id.Value),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
        Assert.False(repository.DeleteCalled);
    }


    [Fact]
    public async Task DeleteRecipeCommandHandler_WhenRecipeIsUsed_ReturnsValidationFailure() {
        var userId = UserId.New();
        var recipe = Recipe.Create(userId, "Used soup", servings: 2);
        SetRecipeUsageCollections(recipe, mealItemsCount: 1, nestedRecipeUsageCount: 0);
        var repository = new SingleRecipeRepository(recipe);
        DeleteRecipeCommandHandler handler = DeleteRecipeHandler(repository, new RecordingCleanupService());

        Result result = await handler.Handle(
            new DeleteRecipeCommand(userId.Value, recipe.Id.Value),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.False(repository.DeleteCalled);
    }


    [Fact]
    public async Task DeleteRecipeCommandValidator_WithEmptyUserId_ReturnsInvalidToken() {
        var validator = new DeleteRecipeCommandValidator();

        ValidationResult result = await validator.ValidateAsync(new DeleteRecipeCommand(Guid.Empty, RecipeId.New().Value));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => string.Equals(error.ErrorCode, "Authentication.InvalidToken", StringComparison.Ordinal));
    }


    [Fact]
    public async Task DeleteRecipeCommandValidator_WhenRecipeIsUsed_HasNoValidationErrors() {
        var userId = UserId.New();
        var recipe = Recipe.Create(userId, "Used soup", servings: 2);
        var validator = new DeleteRecipeCommandValidator();

        ValidationResult result = await validator.ValidateAsync(new DeleteRecipeCommand(userId.Value, recipe.Id.Value));

        Assert.True(result.IsValid);
    }


    [Fact]
    public async Task DeleteRecipeCommandValidator_WhenRecipeIsUnused_HasNoErrors() {
        var userId = UserId.New();
        var recipe = Recipe.Create(userId, "Unused soup", servings: 2);
        var validator = new DeleteRecipeCommandValidator();

        ValidationResult result = await validator.ValidateAsync(new DeleteRecipeCommand(userId.Value, recipe.Id.Value));

        Assert.True(result.IsValid);
    }

}
