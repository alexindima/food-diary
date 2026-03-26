using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Recipes.Commands.UpdateRecipe;
using FoodDiary.Application.Recipes.Common;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Recipes;

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
        Assert.Contains(result.Errors, e =>
            e.PropertyName == "Steps"
            && e.ErrorCode == "Validation.Invalid"
            && e.ErrorMessage == "Step order values must be unique");
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
            Comment: "Comment",
            Category: "Category",
            ImageUrl: null,
            ImageAssetId: null,
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

    private static RecipeStepInput CreateStep(int order, string description) {
        return new RecipeStepInput(
            Order: order,
            Description: description,
            Title: null,
            ImageUrl: null,
            ImageAssetId: null,
            Ingredients: [new RecipeIngredientInput(ProductId: Guid.NewGuid(), NestedRecipeId: null, Amount: 100)]);
    }

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
    }
}
