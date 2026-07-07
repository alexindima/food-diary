using FoodDiary.Application.Abstractions.Recipes.Common;
using FoodDiary.Application.Nutrition.Common;
using FoodDiary.Application.Recipes.Commands.UpdateRecipe;
using FoodDiary.Application.Recipes.Common;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FluentValidation;
using FluentValidation.Results;

namespace FoodDiary.Application.Tests.Recipes;

[ExcludeFromCodeCoverage]
public class UpdateRecipeCommandValidatorTests {
    [Fact]
    public async Task ValidateAsync_WithDuplicateStepOrder_ReturnsValidationError() {
        var userId = UserId.New();
        var recipeId = RecipeId.New();
        var recipe = Recipe.Create(userId, "Soup", servings: 2);
        var validator = new UpdateRecipeCommandValidator(CreateRecipeRepository(recipeId, userId, recipe));

        UpdateRecipeCommand command = CreateCommand(
            userId.Value,
            recipeId,
            [
                CreateStep(order: 1, "Step 1"),
                CreateStep(order: 1, "Step 2 duplicate"),
            ]);

        ValidationResult result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => string.Equals(e.PropertyName, "Steps"
, StringComparison.Ordinal) && string.Equals(e.ErrorCode, "Validation.Invalid"
, StringComparison.Ordinal) && string.Equals(e.ErrorMessage, "Step order values must be unique", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ValidateAsync_WithDistinctEffectiveStepOrder_Passes() {
        var userId = UserId.New();
        var recipeId = RecipeId.New();
        var recipe = Recipe.Create(userId, "Soup", servings: 2);
        var validator = new UpdateRecipeCommandValidator(CreateRecipeRepository(recipeId, userId, recipe));

        UpdateRecipeCommand command = CreateCommand(
            userId.Value,
            recipeId,
            [
                CreateStep(order: 0, "Step uses index fallback to 1"),
                CreateStep(order: 2, "Explicit step 2"),
            ]);

        ValidationResult result = await validator.ValidateAsync(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateAsync_WithManualNutritionAboveMaximum_ReturnsValidationError() {
        var userId = UserId.New();
        var recipeId = RecipeId.New();
        var recipe = Recipe.Create(userId, "Soup", servings: 2);
        var validator = new UpdateRecipeCommandValidator(CreateRecipeRepository(recipeId, userId, recipe, usageCount: 2));
        UpdateRecipeCommand command = CreateCommand(userId.Value, recipeId, [CreateStep(order: 1, "Step")]) with {
            CalculateNutritionAutomatically = false,
            ManualCalories = ManualNutritionLimits.MaxCalories + 1,
            ManualProteins = 10,
            ManualFats = ManualNutritionLimits.MaxNutrient + 1,
            ManualCarbs = 20,
            ManualFiber = 3,
            ManualAlcohol = 0,
        };

        ValidationResult result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => string.Equals(e.PropertyName, nameof(UpdateRecipeCommand.ManualCalories), StringComparison.Ordinal));
        Assert.Contains(result.Errors, e => string.Equals(e.PropertyName, nameof(UpdateRecipeCommand.ManualFats), StringComparison.Ordinal));
    }

    private static UpdateRecipeCommand CreateCommand(
        Guid userId,
        RecipeId recipeId,
        IReadOnlyList<RecipeStepInput> steps) {
        return new UpdateRecipeCommand(
            userId,
            recipeId.Value,
            Name: "Updated",
            Description: "Desc",
            ClearDescription: false,
            Comment: "Comment",
            ClearComment: false,
            Category: "Category",
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
            Steps: steps);
    }

    [Fact]
    public async Task ValidateAsync_WithClearDescriptionAndValue_ReturnsValidationError() {
        var userId = UserId.New();
        var recipeId = RecipeId.New();
        var recipe = Recipe.Create(userId, "Soup", servings: 2);
        var validator = new UpdateRecipeCommandValidator(CreateRecipeRepository(recipeId, userId, recipe));

        UpdateRecipeCommand command = CreateCommand(userId.Value, recipeId, [CreateStep(order: 1, "Step 1")]) with {
            ClearDescription = true,
        };

        ValidationResult result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => string.Equals(e.ErrorMessage, "Description cannot be provided when ClearDescription is true", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ValidateAsync_WithRemainingClearConflicts_ReturnsValidationErrors() {
        var userId = UserId.New();
        var recipeId = RecipeId.New();
        var recipe = Recipe.Create(userId, "Soup", servings: 2);
        var validator = new UpdateRecipeCommandValidator(CreateRecipeRepository(recipeId, userId, recipe));

        UpdateRecipeCommand command = CreateCommand(userId.Value, recipeId, [CreateStep(order: 1, "Step 1")]) with {
            ClearComment = true,
            ClearCategory = true,
            ClearImageUrl = true,
            ImageUrl = "https://cdn.test/soup.png",
            ClearImageAssetId = true,
            ImageAssetId = ImageAssetId.New().Value,
        };

        ValidationResult result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => string.Equals(e.ErrorMessage, "Comment cannot be provided when ClearComment is true", StringComparison.Ordinal));
        Assert.Contains(result.Errors, e => string.Equals(e.ErrorMessage, "Category cannot be provided when ClearCategory is true", StringComparison.Ordinal));
        Assert.Contains(result.Errors, e => string.Equals(e.ErrorMessage, "ImageUrl cannot be provided when ClearImageUrl is true", StringComparison.Ordinal));
        Assert.Contains(result.Errors, e => string.Equals(e.ErrorMessage, "ImageAssetId cannot be provided when ClearImageAssetId is true", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ValidateAsync_WithEmptySteps_ReturnsValidationErrorWithoutDuplicateOrderError() {
        var userId = UserId.New();
        var recipeId = RecipeId.New();
        var recipe = Recipe.Create(userId, "Soup", servings: 2);
        var validator = new UpdateRecipeCommandValidator(CreateRecipeRepository(recipeId, userId, recipe));

        ValidationResult result = await validator.ValidateAsync(CreateCommand(userId.Value, recipeId, []));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => string.Equals(e.ErrorMessage, "Recipe must contain at least one step", StringComparison.Ordinal));
        Assert.DoesNotContain(result.Errors, e => string.Equals(e.ErrorMessage, "Step order values must be unique", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ValidateAsync_WithInvalidUserId_DoesNotQueryRecipeRepository() {
        var validator = new UpdateRecipeCommandValidator(CreateThrowingRecipeRepository());

        ValidationResult result = await validator.ValidateAsync(CreateCommand(Guid.Empty, RecipeId.New(), [CreateStep(order: 1, "Step 1")]));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => string.Equals(e.ErrorCode, "Authentication.InvalidToken", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ValidateAsync_WhenRepositoryRecipeIsUsed_ReturnsValidationError() {
        var userId = UserId.New();
        var recipeId = RecipeId.New();
        var recipe = Recipe.Create(userId, "Used soup", servings: 2);
        SetRecipeUsageCollections(recipe, mealItemsCount: 1, nestedRecipeUsageCount: 1);
        var validator = new UpdateRecipeCommandValidator(CreateRecipeRepository(recipeId, userId, recipe, usageCount: 2));

        ValidationResult result = await validator.ValidateAsync(CreateCommand(userId.Value, recipeId, [CreateStep(order: 1, "Step 1")]));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => string.Equals(e.ErrorCode, "Validation.Invalid", StringComparison.Ordinal) &&
            string.Equals(e.ErrorMessage, "Recipe is already used and cannot be modified", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ValidateAsync_WithCachedUsedRecipe_ReturnsValidationErrorFromUsageCount() {
        var userId = UserId.New();
        var recipeId = RecipeId.New();
        var recipe = Recipe.Create(userId, "Cached used soup", servings: 2);
        SetRecipeUsageCollections(recipe, mealItemsCount: 1, nestedRecipeUsageCount: 0);
        var validator = new UpdateRecipeCommandValidator(CreateUsageCountRecipeRepository(recipe.Id, userId, usageCount: 1));
        UpdateRecipeCommand command = CreateCommand(userId.Value, recipeId, [CreateStep(order: 1, "Step 1")]);
        var context = new ValidationContext<UpdateRecipeCommand>(command);
        context.RootContextData["__recipe"] = recipe;

        ValidationResult result = await validator.ValidateAsync(context);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => string.Equals(e.ErrorCode, "Validation.Invalid", StringComparison.Ordinal) &&
            string.Equals(e.ErrorMessage, "Recipe is already used and cannot be modified", StringComparison.Ordinal));
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

    private static void SetRecipeUsageCollections(Recipe recipe, int mealItemsCount, int nestedRecipeUsageCount) {
        var mealItems = Enumerable.Range(0, mealItemsCount)
            .Select(_ => (MealItem)null!)
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

    private static IRecipeRepository CreateRecipeRepository(RecipeId recipeId, UserId userId, Recipe recipe, int usageCount = 0) {
        IRecipeRepository repository = Substitute.For<IRecipeRepository>();
        repository
            .GetByIdAsync(
                Arg.Any<RecipeId>(),
                Arg.Any<UserId>(),
                Arg.Any<bool>(),
                Arg.Any<bool>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(call => {
                RecipeId id = call.ArgAt<RecipeId>(0);
                UserId requestedUserId = call.ArgAt<UserId>(1);
                return Task.FromResult(id == recipeId && requestedUserId == userId ? recipe : null);
            });
        repository
            .GetUsageCountAsync(
                Arg.Any<RecipeId>(),
                Arg.Any<UserId>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(call => {
                RecipeId id = call.ArgAt<RecipeId>(0);
                UserId requestedUserId = call.ArgAt<UserId>(1);
                bool isRequestedRecipe = id == recipeId || id == recipe.Id;
                return Task.FromResult(isRequestedRecipe && requestedUserId == userId ? usageCount : 0);
            });

        return repository;
    }

    private static IRecipeRepository CreateUsageCountRecipeRepository(RecipeId recipeId, UserId userId, int usageCount) {
        IRecipeRepository repository = Substitute.For<IRecipeRepository>();
        repository
            .GetByIdAsync(
                Arg.Any<RecipeId>(),
                Arg.Any<UserId>(),
                Arg.Any<bool>(),
                Arg.Any<bool>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns<Task<Recipe?>>(_ => throw new InvalidOperationException("Recipe should be read from validator context."));
        repository
            .GetUsageCountAsync(
                Arg.Any<RecipeId>(),
                Arg.Any<UserId>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(call => {
                RecipeId id = call.ArgAt<RecipeId>(0);
                UserId requestedUserId = call.ArgAt<UserId>(1);
                return Task.FromResult(id == recipeId && requestedUserId == userId ? usageCount : 0);
            });
        return repository;
    }

    private static IRecipeRepository CreateThrowingRecipeRepository() {
        IRecipeRepository repository = Substitute.For<IRecipeRepository>();
        repository
            .GetByIdAsync(
                Arg.Any<RecipeId>(),
                Arg.Any<UserId>(),
                Arg.Any<bool>(),
                Arg.Any<bool>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns<Task<Recipe?>>(_ => throw new InvalidOperationException("Repository should not be queried."));
        repository
            .GetUsageCountAsync(
                Arg.Any<RecipeId>(),
                Arg.Any<UserId>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns<Task<int>>(_ => throw new InvalidOperationException("Repository should not be queried."));

        return repository;
    }
}
