using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Application.Recipes.Commands.DeleteRecipe;
using FoodDiary.Application.Recipes.Queries.GetRecentRecipes;
using FoodDiary.Application.Recipes.Queries.GetRecipesWithRecent;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Recipes;

public class RecipesFeatureTests {
    [Fact]
    public async Task GetRecentRecipesQueryValidator_WithEmptyUserId_Fails() {
        var validator = new GetRecentRecipesQueryValidator();
        var query = new GetRecentRecipesQuery(Guid.Empty, 10, true);

        var result = await validator.ValidateAsync(query);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task GetRecipesWithRecentQueryValidator_WithValidUserId_Passes() {
        var validator = new GetRecipesWithRecentQueryValidator();
        var query = new GetRecipesWithRecentQuery(Guid.NewGuid(), 1, 10, null, true, 10);

        var result = await validator.ValidateAsync(query);

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

        var result = await handler.Handle(new DeleteRecipeCommand(userId.Value, recipe.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
        Assert.True(repository.DeleteCalled);
        Assert.Equal([recipeAssetId, stepAssetId], cleanup.RequestedAssetIds);
    }

    private sealed class SingleRecipeRepository(Recipe recipe) : IRecipeRepository {
        public bool DeleteCalled { get; private set; }

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
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Recipe?>(id == recipe.Id && userId == recipe.UserId ? recipe : null);

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

        public Task DeleteAsync(Recipe recipe, CancellationToken cancellationToken = default) {
            DeleteCalled = true;
            return Task.CompletedTask;
        }

        public Task UpdateNutritionAsync(Recipe recipe, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class RecordingCleanupService(string? errorCode = null) : IImageAssetCleanupService {
        public List<ImageAssetId> RequestedAssetIds { get; } = [];

        public Task<DeleteImageAssetResult> DeleteIfUnusedAsync(ImageAssetId assetId, CancellationToken cancellationToken = default) {
            RequestedAssetIds.Add(assetId);
            return Task.FromResult(errorCode is null
                ? new DeleteImageAssetResult(true)
                : new DeleteImageAssetResult(false, errorCode));
        }

        public Task<int> CleanupOrphansAsync(DateTime olderThanUtc, int batchSize, CancellationToken cancellationToken = default) =>
            Task.FromResult(0);
    }
}
