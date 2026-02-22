using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Application.Recipes.Commands.UpdateRecipe;
using FoodDiary.Application.Recipes.Common;
using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;

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
            new NoopImageAssetRepository(),
            new NoopImageStorageService());

        var command = new UpdateRecipeCommand(
            userId,
            recipeId,
            Name: "Updated soup",
            Description: null,
            Comment: null,
            Category: null,
            ImageUrl: null,
            ImageAssetId: null,
            PrepTime: 10,
            CookTime: 20,
            Servings: 2,
            Visibility: Visibility.PUBLIC.ToString(),
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

        public Task<Recipe> AddAsync(Recipe recipe) => throw new NotSupportedException();

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

        public Task UpdateAsync(Recipe recipe) => Task.CompletedTask;

        public Task DeleteAsync(Recipe recipe) => throw new NotSupportedException();

        public Task UpdateNutritionAsync(Recipe recipe, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class NoopImageAssetRepository : IImageAssetRepository {
        public Task<ImageAsset> AddAsync(ImageAsset asset, CancellationToken cancellationToken = default) {
            throw new NotSupportedException();
        }

        public Task<ImageAsset?> GetByIdAsync(ImageAssetId id, CancellationToken cancellationToken = default) {
            return Task.FromResult<ImageAsset?>(null);
        }

        public Task DeleteAsync(ImageAsset asset, CancellationToken cancellationToken = default) {
            return Task.CompletedTask;
        }

        public Task<bool> IsAssetInUse(ImageAssetId assetId, CancellationToken cancellationToken = default) {
            return Task.FromResult(false);
        }

        public Task<IReadOnlyList<ImageAsset>> GetUnusedOlderThanAsync(
            DateTime olderThanUtc,
            int batchSize,
            CancellationToken cancellationToken = default) {
            throw new NotSupportedException();
        }
    }

    private sealed class NoopImageStorageService : IImageStorageService {
        public Task<PresignedUpload> CreatePresignedUploadAsync(
            UserId userId,
            string fileName,
            string contentType,
            long fileSizeBytes,
            CancellationToken cancellationToken) {
            throw new NotSupportedException();
        }

        public Task DeleteAsync(string objectKey, CancellationToken cancellationToken) {
            return Task.CompletedTask;
        }
    }
}
