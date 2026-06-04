using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Recipes.Commands.UpdateRecipe;
using FoodDiary.Application.Recipes.Common;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FluentValidation;

namespace FoodDiary.Application.Tests.Recipes;

[ExcludeFromCodeCoverage]
public class UpdateRecipeCommandValidatorTests {
    [Fact]
    public async Task ValidateAsync_WithDuplicateStepOrder_ReturnsValidationError() {
        var userId = UserId.New();
        var recipeId = RecipeId.New();
        var recipe = Recipe.Create(userId, "Soup", servings: 2);
        var validator = new UpdateRecipeCommandValidator(new StubRecipeRepository(recipeId, userId, recipe));

        var command = CreateCommand(
            userId.Value,
            recipeId,
            [
                CreateStep(order: 1, "Step 1"),
                CreateStep(order: 1, "Step 2 duplicate")
            ]);

        var result = await validator.ValidateAsync(command);

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
        var validator = new UpdateRecipeCommandValidator(new StubRecipeRepository(recipeId, userId, recipe));

        var command = CreateCommand(
            userId.Value,
            recipeId,
            [
                CreateStep(order: 0, "Step uses index fallback to 1"),
                CreateStep(order: 2, "Explicit step 2")
            ]);

        var result = await validator.ValidateAsync(command);

        Assert.True(result.IsValid);
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
        var validator = new UpdateRecipeCommandValidator(new StubRecipeRepository(recipeId, userId, recipe));

        var command = CreateCommand(userId.Value, recipeId, [CreateStep(order: 1, "Step 1")]) with {
            ClearDescription = true
        };

        var result = await validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => string.Equals(e.ErrorMessage, "Description cannot be provided when ClearDescription is true", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ValidateAsync_WithRemainingClearConflicts_ReturnsValidationErrors() {
        var userId = UserId.New();
        var recipeId = RecipeId.New();
        var recipe = Recipe.Create(userId, "Soup", servings: 2);
        var validator = new UpdateRecipeCommandValidator(new StubRecipeRepository(recipeId, userId, recipe));

        var command = CreateCommand(userId.Value, recipeId, [CreateStep(order: 1, "Step 1")]) with {
            ClearComment = true,
            ClearCategory = true,
            ClearImageUrl = true,
            ImageUrl = "https://cdn.test/soup.png",
            ClearImageAssetId = true,
            ImageAssetId = ImageAssetId.New().Value
        };

        var result = await validator.ValidateAsync(command);

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
        var validator = new UpdateRecipeCommandValidator(new StubRecipeRepository(recipeId, userId, recipe));

        var result = await validator.ValidateAsync(CreateCommand(userId.Value, recipeId, []));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => string.Equals(e.ErrorMessage, "Recipe must contain at least one step", StringComparison.Ordinal));
        Assert.DoesNotContain(result.Errors, e => string.Equals(e.ErrorMessage, "Step order values must be unique", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ValidateAsync_WithInvalidUserId_DoesNotQueryRecipeRepository() {
        var validator = new UpdateRecipeCommandValidator(new ThrowingRecipeRepository());

        var result = await validator.ValidateAsync(CreateCommand(Guid.Empty, RecipeId.New(), [CreateStep(order: 1, "Step 1")]));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => string.Equals(e.ErrorCode, "Authentication.InvalidToken", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ValidateAsync_WhenRepositoryRecipeIsUsed_ReturnsValidationError() {
        var userId = UserId.New();
        var recipeId = RecipeId.New();
        var recipe = Recipe.Create(userId, "Used soup", servings: 2);
        SetRecipeUsageCollections(recipe, mealItemsCount: 1, nestedRecipeUsageCount: 1);
        var validator = new UpdateRecipeCommandValidator(new StubRecipeRepository(recipeId, userId, recipe));

        var result = await validator.ValidateAsync(CreateCommand(userId.Value, recipeId, [CreateStep(order: 1, "Step 1")]));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => string.Equals(e.ErrorCode, "Validation.Invalid", StringComparison.Ordinal) &&
            string.Equals(e.ErrorMessage, "Recipe is already used and cannot be modified", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ValidateAsync_WithCachedUsedRecipe_ReturnsValidationErrorWithoutRepositoryQuery() {
        var userId = UserId.New();
        var recipeId = RecipeId.New();
        var recipe = Recipe.Create(userId, "Cached used soup", servings: 2);
        SetRecipeUsageCollections(recipe, mealItemsCount: 1, nestedRecipeUsageCount: 0);
        var validator = new UpdateRecipeCommandValidator(new ThrowingRecipeRepository());
        var command = CreateCommand(userId.Value, recipeId, [CreateStep(order: 1, "Step 1")]);
        var context = new ValidationContext<UpdateRecipeCommand>(command);
        context.RootContextData["__recipe"] = recipe;

        var result = await validator.ValidateAsync(context);

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

    [ExcludeFromCodeCoverage]
    private sealed class StubRecipeRepository : IRecipeRepository {
        private readonly RecipeId _recipeId;
        private readonly UserId _userId;
        private readonly Recipe _recipe;

        public StubRecipeRepository(RecipeId recipeId, UserId userId, Recipe recipe) {
            _recipeId = recipeId;
            _userId = userId;
            _recipe = recipe;
        }

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
    private sealed class ThrowingRecipeRepository : IRecipeRepository {
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
            CancellationToken cancellationToken = default) => throw new InvalidOperationException("Repository should not be queried.");

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

        public Task UpdateNutritionAsync(Recipe recipe, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<(IReadOnlyList<(Recipe Recipe, int UsageCount)> Items, int TotalItems)> GetExplorePagedAsync(
            int page, int limit, string? search, string? category, int? maxPrepTime, string sortBy,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
