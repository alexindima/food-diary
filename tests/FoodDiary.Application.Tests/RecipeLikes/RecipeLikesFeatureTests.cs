using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.RecipeLikes.Commands.ToggleRecipeLike;
using FoodDiary.Application.RecipeLikes.Common;
using FoodDiary.Application.RecipeLikes.Queries.GetRecipeLikeStatus;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Entities.Social;

using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.RecipeLikes;

public class RecipeLikesFeatureTests {
    [Fact]
    public async Task ToggleRecipeLike_WhenNotLiked_AddsLike() {
        var userId = UserId.New();
        var recipe = Recipe.Create(userId, "Pasta", 1);
        var repo = new InMemoryRecipeLikeRepository();
        var recipeRepo = new StubRecipeRepository(recipe);

        var handler = new ToggleRecipeLikeCommandHandler(repo, recipeRepo);
        var result = await handler.Handle(
            new ToggleRecipeLikeCommand(userId.Value, recipe.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsLiked);
        Assert.Equal(1, result.Value.TotalLikes);
    }

    [Fact]
    public async Task ToggleRecipeLike_WhenAlreadyLiked_RemovesLike() {
        var userId = UserId.New();
        var recipe = Recipe.Create(userId, "Pasta", 1);
        var repo = new InMemoryRecipeLikeRepository();
        repo.Seed(RecipeLike.Create(userId, recipe.Id));
        var recipeRepo = new StubRecipeRepository(recipe);

        var handler = new ToggleRecipeLikeCommandHandler(repo, recipeRepo);
        var result = await handler.Handle(
            new ToggleRecipeLikeCommand(userId.Value, recipe.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsLiked);
        Assert.Equal(0, result.Value.TotalLikes);
    }

    [Fact]
    public async Task ToggleRecipeLike_WhenRecipeNotFound_ReturnsFailure() {
        var repo = new InMemoryRecipeLikeRepository();
        var recipeRepo = new StubRecipeRepository(null);

        var handler = new ToggleRecipeLikeCommandHandler(repo, recipeRepo);
        var result = await handler.Handle(
            new ToggleRecipeLikeCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("NotFound", result.Error.Code);
    }

    [Fact]
    public async Task ToggleRecipeLike_WithNullUserId_ReturnsFailure() {
        var handler = new ToggleRecipeLikeCommandHandler(
            new InMemoryRecipeLikeRepository(), new StubRecipeRepository(null));

        var result = await handler.Handle(
            new ToggleRecipeLikeCommand(null, Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task GetRecipeLikeStatus_ReturnsCorrectStatus() {
        var userId = UserId.New();
        var recipeId = RecipeId.New();
        var repo = new InMemoryRecipeLikeRepository();
        repo.Seed(RecipeLike.Create(userId, recipeId));

        var handler = new GetRecipeLikeStatusQueryHandler(repo);
        var result = await handler.Handle(
            new GetRecipeLikeStatusQuery(userId.Value, recipeId.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsLiked);
        Assert.Equal(1, result.Value.TotalLikes);
    }

    [Fact]
    public async Task GetRecipeLikeStatus_WhenNotLiked_ReturnsFalse() {
        var repo = new InMemoryRecipeLikeRepository();

        var handler = new GetRecipeLikeStatusQueryHandler(repo);
        var result = await handler.Handle(
            new GetRecipeLikeStatusQuery(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsLiked);
        Assert.Equal(0, result.Value.TotalLikes);
    }

    private sealed class InMemoryRecipeLikeRepository : IRecipeLikeRepository {
        private readonly List<RecipeLike> _likes = [];

        public void Seed(RecipeLike like) => _likes.Add(like);

        public Task<RecipeLike?> GetByUserAndRecipeAsync(UserId userId, RecipeId recipeId, CancellationToken ct = default) =>
            Task.FromResult(_likes.FirstOrDefault(l => l.UserId == userId && l.RecipeId == recipeId));

        public Task<RecipeLike> AddAsync(RecipeLike like, CancellationToken ct = default) {
            _likes.Add(like);
            return Task.FromResult(like);
        }

        public Task DeleteAsync(RecipeLike like, CancellationToken ct = default) {
            _likes.Remove(like);
            return Task.CompletedTask;
        }

        public Task<int> CountByRecipeAsync(RecipeId recipeId, CancellationToken ct = default) =>
            Task.FromResult(_likes.Count(l => l.RecipeId == recipeId));
    }

    private sealed class StubRecipeRepository(Recipe? recipe) : IRecipeRepository {
        public Task<Recipe?> GetByIdAsync(RecipeId id, UserId userId, bool includePublic = true,
            bool includeSteps = false, bool asTracking = false, CancellationToken cancellationToken = default) =>
            Task.FromResult(recipe);

        public Task<Recipe> AddAsync(Recipe r, CancellationToken ct = default) => throw new NotSupportedException();
        public Task UpdateAsync(Recipe r, CancellationToken ct = default) => throw new NotSupportedException();
        public Task DeleteAsync(Recipe r, CancellationToken ct = default) => throw new NotSupportedException();
        public Task UpdateNutritionAsync(Recipe r, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<(Recipe Recipe, int UsageCount)> Items, int TotalItems)> GetPagedAsync(UserId userId, bool includePublic, int page, int limit, string? search, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyDictionary<RecipeId, Recipe>> GetByIdsAsync(IEnumerable<RecipeId> ids, UserId userId, bool includePublic = true, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyDictionary<RecipeId, (Recipe Recipe, int UsageCount)>> GetByIdsWithUsageAsync(IEnumerable<RecipeId> ids, UserId userId, bool includePublic = true, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<(Recipe Recipe, int UsageCount)> Items, int TotalItems)> GetExplorePagedAsync(int page, int limit, string? search, string? category, int? maxPrepTime, string sortBy, CancellationToken ct = default) => throw new NotSupportedException();
    }
}
