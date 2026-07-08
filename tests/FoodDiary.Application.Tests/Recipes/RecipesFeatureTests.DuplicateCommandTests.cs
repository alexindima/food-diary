using FoodDiary.Results;
using FoodDiary.Application.Recipes.Commands.DuplicateRecipe;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Recipes.Models;

namespace FoodDiary.Application.Tests.Recipes;

public partial class RecipesFeatureTests {

    [Fact]
    public async Task DuplicateRecipeCommandHandler_WithEmptyRecipeId_ReturnsValidationFailure() {
        DuplicateRecipeCommandHandler handler = DuplicateRecipeHandler(new SingleRecipeRepositoryForCreate());

        Result<RecipeModel> result = await handler.Handle(
            new DuplicateRecipeCommand(Guid.NewGuid(), Guid.Empty),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("RecipeId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }


    [Fact]
    public async Task DuplicateRecipeCommandHandler_WithEmptyUserId_ReturnsInvalidToken() {
        DuplicateRecipeCommandHandler handler = DuplicateRecipeHandler(new SingleRecipeRepositoryForCreate());

        Result<RecipeModel> result = await handler.Handle(
            new DuplicateRecipeCommand(Guid.Empty, Guid.NewGuid()),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }


    [Fact]
    public async Task DuplicateRecipeCommandHandler_WhenOriginalRecipeIsMissing_ReturnsNotAccessible() {
        var user = User.Create("duplicate-recipe-missing-original@example.com", "hash");
        DuplicateRecipeCommandHandler handler = DuplicateRecipeHandler(
            new SingleRecipeRepositoryForCreate(),
            new StubUserRepository(user));

        Result<RecipeModel> result = await handler.Handle(
            new DuplicateRecipeCommand(user.Id.Value, Guid.NewGuid()),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Recipe.NotAccessible", result.Error.Code);
    }


    [Fact]
    public async Task DuplicateRecipeCommandHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("duplicate-recipe-deleted@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        DuplicateRecipeCommandHandler handler = DuplicateRecipeHandler(
            new SingleRecipeRepositoryForCreate(),
            new StubUserRepository(user));

        Result<RecipeModel> result = await handler.Handle(
            new DuplicateRecipeCommand(user.Id.Value, Guid.NewGuid()),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
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
        DuplicateRecipeCommandHandler handler = DuplicateRecipeHandler(repository);

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
        DuplicateRecipeCommandHandler handler = DuplicateRecipeHandler(repository);

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
        DuplicateRecipeCommandHandler handler = DuplicateRecipeHandler(repository);

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
        DuplicateRecipeCommandHandler handler = DuplicateRecipeHandler(repository);

        Result<RecipeModel> result = await handler.Handle(new DuplicateRecipeCommand(user.Id.Value, original.Id.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal("Original Recipe", result.Value.Name);
        Assert.NotNull(repository.LastAddedRecipe);
        Assert.NotEqual(original.Id.Value, result.Value.Id);
    }

}
