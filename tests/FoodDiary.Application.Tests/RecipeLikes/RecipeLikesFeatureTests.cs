using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.RecipeLikes.Commands.ToggleRecipeLike;
using FoodDiary.Application.Abstractions.RecipeLikes.Common;
using FoodDiary.Application.RecipeLikes.Queries.GetRecipeLikeStatus;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Entities.Social;

using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.RecipeLikes.Models;

namespace FoodDiary.Application.Tests.RecipeLikes;

[ExcludeFromCodeCoverage]
public class RecipeLikesFeatureTests {
    [Fact]
    public async Task ToggleRecipeLike_WhenNotLiked_AddsLike() {
        var userId = UserId.New();
        var recipe = Recipe.Create(userId, "Pasta", 1);
        var repo = new InMemoryRecipeLikeRepository();
        IRecipeRepository recipeRepo = CreateRecipeRepository(recipe);

        var handler = new ToggleRecipeLikeCommandHandler(repo, recipeRepo);
        Result<RecipeLikeStatusModel> result = await handler.Handle(
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
        IRecipeRepository recipeRepo = CreateRecipeRepository(recipe);

        var handler = new ToggleRecipeLikeCommandHandler(repo, recipeRepo);
        Result<RecipeLikeStatusModel> result = await handler.Handle(
            new ToggleRecipeLikeCommand(userId.Value, recipe.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsLiked);
        Assert.Equal(0, result.Value.TotalLikes);
    }

    [Fact]
    public async Task ToggleRecipeLike_WhenRecipeNotFound_ReturnsFailure() {
        var repo = new InMemoryRecipeLikeRepository();
        IRecipeRepository recipeRepo = CreateRecipeRepository(recipe: null);

        var handler = new ToggleRecipeLikeCommandHandler(repo, recipeRepo);
        Result<RecipeLikeStatusModel> result = await handler.Handle(
            new ToggleRecipeLikeCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("NotFound", result.Error.Code, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ToggleRecipeLike_WithNullUserId_ReturnsFailure() {
        var handler = new ToggleRecipeLikeCommandHandler(
            new InMemoryRecipeLikeRepository(), CreateRecipeRepository(recipe: null));

        Result<RecipeLikeStatusModel> result = await handler.Handle(
            new ToggleRecipeLikeCommand(UserId: null, Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task GetRecipeLikeStatus_ReturnsCorrectStatus() {
        var userId = UserId.New();
        var recipeId = RecipeId.New();
        var repo = new InMemoryRecipeLikeRepository();
        repo.Seed(RecipeLike.Create(userId, recipeId));

        var handler = new GetRecipeLikeStatusQueryHandler(repo);
        Result<RecipeLikeStatusModel> result = await handler.Handle(
            new GetRecipeLikeStatusQuery(userId.Value, recipeId.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsLiked);
        Assert.Equal(1, result.Value.TotalLikes);
    }

    [Fact]
    public async Task GetRecipeLikeStatus_WhenNotLiked_ReturnsFalse() {
        var repo = new InMemoryRecipeLikeRepository();

        var handler = new GetRecipeLikeStatusQueryHandler(repo);
        Result<RecipeLikeStatusModel> result = await handler.Handle(
            new GetRecipeLikeStatusQuery(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsLiked);
        Assert.Equal(0, result.Value.TotalLikes);
    }

    [Fact]
    public async Task GetRecipeLikeStatus_WithNullUserId_ReturnsFailure() {
        var handler = new GetRecipeLikeStatusQueryHandler(new InMemoryRecipeLikeRepository());

        Result<RecipeLikeStatusModel> result = await handler.Handle(
            new GetRecipeLikeStatusQuery(UserId: null, Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [ExcludeFromCodeCoverage]
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

    private static IRecipeRepository CreateRecipeRepository(Recipe? recipe) {
        IRecipeRepository repository = Substitute.For<IRecipeRepository>();
        repository
            .GetByIdAsync(
                Arg.Any<RecipeId>(),
                Arg.Any<UserId>(),
                Arg.Any<bool>(),
                Arg.Any<bool>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(recipe));
        return repository;
    }
}
