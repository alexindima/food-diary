using FoodDiary.Application.RecipeLikes.Commands.ToggleRecipeLike;
using FoodDiary.Application.Abstractions.RecipeLikes.Common;
using FoodDiary.Application.Abstractions.Recipes.Common;
using FoodDiary.Application.RecipeLikes.Common;
using FoodDiary.Application.RecipeLikes.Queries.GetRecipeLikeStatus;
using FoodDiary.Application.RecipeLikes.Services;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Entities.Social;

using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;
using FoodDiary.Application.RecipeLikes.Models;

namespace FoodDiary.Application.Tests.RecipeLikes;

[ExcludeFromCodeCoverage]
public class RecipeLikesFeatureTests {
    [Fact]
    public async Task ToggleRecipeLike_WhenNotLiked_AddsLike() {
        var userId = UserId.New();
        var recipe = Recipe.Create(userId, "Pasta", 1);
        var repo = new InMemoryRecipeLikeRepository();
        IRecipeAccessService recipeAccessService = CreateRecipeAccessService(recipe);

        var handler = new ToggleRecipeLikeCommandHandler(repo, recipeAccessService);
        Result<RecipeLikeStatusModel> result = await handler.Handle(
            new ToggleRecipeLikeCommand(userId.Value, recipe.Id.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.True(result.Value.IsLiked);
        Assert.Equal(1, result.Value.TotalLikes);
    }

    [Fact]
    public async Task ToggleRecipeLike_WhenAlreadyLiked_RemovesLike() {
        var userId = UserId.New();
        var recipe = Recipe.Create(userId, "Pasta", 1);
        var repo = new InMemoryRecipeLikeRepository();
        repo.Seed(RecipeLike.Create(userId, recipe.Id));
        IRecipeAccessService recipeAccessService = CreateRecipeAccessService(recipe);

        var handler = new ToggleRecipeLikeCommandHandler(repo, recipeAccessService);
        Result<RecipeLikeStatusModel> result = await handler.Handle(
            new ToggleRecipeLikeCommand(userId.Value, recipe.Id.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.False(result.Value.IsLiked);
        Assert.Equal(0, result.Value.TotalLikes);
    }

    [Fact]
    public async Task ToggleRecipeLike_WhenRecipeNotFound_ReturnsFailure() {
        var repo = new InMemoryRecipeLikeRepository();
        IRecipeAccessService recipeAccessService = CreateRecipeAccessService(recipe: null);

        var handler = new ToggleRecipeLikeCommandHandler(repo, recipeAccessService);
        Result<RecipeLikeStatusModel> result = await handler.Handle(
            new ToggleRecipeLikeCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Contains("NotFound", result.Error.Code, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ToggleRecipeLike_WithNullUserId_ReturnsFailure() {
        var handler = new ToggleRecipeLikeCommandHandler(
            new InMemoryRecipeLikeRepository(),
            CreateRecipeAccessService(recipe: null));

        Result<RecipeLikeStatusModel> result = await handler.Handle(
            new ToggleRecipeLikeCommand(UserId: null, Guid.NewGuid()), CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task GetRecipeLikeStatus_ReturnsCorrectStatus() {
        var userId = UserId.New();
        var recipeId = RecipeId.New();
        var repo = new InMemoryRecipeLikeRepository();
        repo.Seed(RecipeLike.Create(userId, recipeId));

        GetRecipeLikeStatusQueryHandler handler = CreateRecipeLikeStatusHandler(repo);
        Result<RecipeLikeStatusModel> result = await handler.Handle(
            new GetRecipeLikeStatusQuery(userId.Value, recipeId.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.True(result.Value.IsLiked);
        Assert.Equal(1, result.Value.TotalLikes);
    }

    [Fact]
    public async Task GetRecipeLikeStatus_WhenNotLiked_ReturnsFalse() {
        var repo = new InMemoryRecipeLikeRepository();

        GetRecipeLikeStatusQueryHandler handler = CreateRecipeLikeStatusHandler(repo);
        Result<RecipeLikeStatusModel> result = await handler.Handle(
            new GetRecipeLikeStatusQuery(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.False(result.Value.IsLiked);
        Assert.Equal(0, result.Value.TotalLikes);
    }

    [Fact]
    public async Task GetRecipeLikeStatus_WithNullUserId_ReturnsFailure() {
        GetRecipeLikeStatusQueryHandler handler = CreateRecipeLikeStatusHandler(new InMemoryRecipeLikeRepository());

        Result<RecipeLikeStatusModel> result = await handler.Handle(
            new GetRecipeLikeStatusQuery(UserId: null, Guid.NewGuid()), CancellationToken.None);

        ResultAssert.Failure(result);
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

    private static IRecipeAccessService CreateRecipeAccessService(Recipe? recipe) {
        IRecipeAccessService service = Substitute.For<IRecipeAccessService>();
        service
            .GetAccessibleByIdAsync(
                Arg.Any<RecipeId>(),
                Arg.Any<UserId>(),
                Arg.Any<bool>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(recipe));
        return service;
    }

    private static GetRecipeLikeStatusQueryHandler CreateRecipeLikeStatusHandler(
        IRecipeLikeReadRepository likeRepository) =>
        new(CreateRecipeLikeReadService(likeRepository));

    private static IRecipeLikeReadService CreateRecipeLikeReadService(
        IRecipeLikeReadRepository likeRepository) =>
        new RecipeLikeReadService(likeRepository);
}
