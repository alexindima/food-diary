using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Images.Common;
using FoodDiary.Application.Recipes.Commands.UpdateRecipe;
using FoodDiary.Application.Recipes.Common;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Recipes;

public class UpdateRecipeCommandHandlerTests {
    [Fact]
    public async Task Handle_WithDuplicateStepOrder_ThrowsArgumentException() {
        var userId = UserId.New();
        var recipeId = RecipeId.New();
        var recipe = Recipe.Create(userId, "Soup", servings: 2);
        recipe.AddStep(1, "Initial step");

        var repository = new StubRecipeRepository(recipeId, userId, recipe);
        var handler = new UpdateRecipeCommandHandler(
            repository,
            new NoopImageAssetCleanupService());

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
                CreateStep(order: 1, "Step 2 duplicate order")
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
            new NoopImageAssetCleanupService());

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
            new NoopImageAssetCleanupService());

        var result = await handler.Handle(
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

    [Fact]
    public async Task Handle_WithEmptyImageAssetId_ReturnsValidationFailure() {
        var userId = UserId.New();
        var recipeId = RecipeId.New();
        var recipe = Recipe.Create(userId, "Soup", servings: 2);
        recipe.AddStep(1, "Initial step");

        var repository = new StubRecipeRepository(recipeId, userId, recipe);
        var handler = new UpdateRecipeCommandHandler(
            repository,
            new NoopImageAssetCleanupService());

        var result = await handler.Handle(
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
    public async Task Handle_WithEmptyNestedRecipeId_ReturnsValidationFailure() {
        var userId = UserId.New();
        var recipeId = RecipeId.New();
        var recipe = Recipe.Create(userId, "Soup", servings: 2);
        recipe.AddStep(1, "Initial step");

        var repository = new StubRecipeRepository(recipeId, userId, recipe);
        var handler = new UpdateRecipeCommandHandler(
            repository,
            new NoopImageAssetCleanupService());

        var result = await handler.Handle(
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

    private sealed class NoopImageAssetCleanupService : IImageAssetCleanupService {
        public Task<DeleteImageAssetResult> DeleteIfUnusedAsync(ImageAssetId assetId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new DeleteImageAssetResult(true));

        public Task<int> CleanupOrphansAsync(DateTime olderThanUtc, int batchSize, CancellationToken cancellationToken = default) =>
            Task.FromResult(0);
    }
}
