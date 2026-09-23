using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteRecipes.Common;
using FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteRecipes.Models;
using FoodDiary.Modules.Favorites.Application.FavoriteRecipes.Queries.GetFavoriteRecipePage;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Favorites.Application.Tests.FavoriteRecipes;

[ExcludeFromCodeCoverage]
public sealed class GetFavoriteRecipePageTests {
    [Fact]
    public async Task ResolvesOwnerAndForwardsPageSearchAndCancellationAsync() {
        var owner = UserId.New();
        using var cancellation = new CancellationTokenSource();
        ICurrentUserAccessService access = Substitute.For<ICurrentUserAccessService>();
        access.EnsureCanAccessAsync(owner, cancellation.Token).Returns((Error?)null);
        IFavoriteRecipeQuery repository = Substitute.For<IFavoriteRecipeQuery>();
        var item = new FavoriteRecipeReadModel(Guid.NewGuid(), Guid.NewGuid(), "Lunch", DateTime.UtcNow, "Rice", "https://example.com/rice.jpg", 500, 2, 10, 20, 1) {
            TotalProteins = 20,
            TotalFats = 10,
            TotalCarbs = 30,
            TotalFiber = 7.5,
            IngredientNames = ["Rice"],
        };
        repository.GetPageReadModelsAsync(owner, 2, 10, "rice", cancellation.Token).Returns((new[] { item }, 23));
        var handler = new GetFavoriteRecipePageQueryHandler(repository, access);
        FoodDiary.Application.Abstractions.Common.Models.PagedResponse<FoodDiary.Modules.Favorites.Contracts.FavoriteRecipes.Models.FavoriteRecipeModel> result = ResultAssert.Success(await handler.Handle(new GetFavoriteRecipePageQuery(owner.Value, 2, 10, " rice "), cancellation.Token));
        Assert.Multiple(
            () => Assert.Equal("https://example.com/rice.jpg", result.Data.Single().ImageUrl),
            () => Assert.Equal(7.5, result.Data.Single().TotalFiber),
            () => Assert.Equal(23, result.TotalItems),
            () => Assert.Equal(3, result.TotalPages),
            () => Assert.Equal(2, result.Page),
            () => Assert.Equal(new[] { "Rice" }, result.Data.Single().IngredientNames));
        await access.Received(1).EnsureCanAccessAsync(owner, cancellation.Token);
    }

    [Fact]
    public async Task DeniedOwnerDoesNotReadFavoritesAsync() {
        var owner = UserId.New();
        ICurrentUserAccessService access = Substitute.For<ICurrentUserAccessService>();
        access.EnsureCanAccessAsync(owner, Arg.Any<CancellationToken>()).Returns(AuthenticationErrors.InvalidToken);
        IFavoriteRecipeQuery repository = Substitute.For<IFavoriteRecipeQuery>();
        var handler = new GetFavoriteRecipePageQueryHandler(repository, access);
        ResultAssert.Failure(await handler.Handle(new GetFavoriteRecipePageQuery(owner.Value), CancellationToken.None));
        Assert.Empty(repository.ReceivedCalls());
    }

    [Theory]
    [InlineData(0, 10, 0)]
    [InlineData(10001, 10, 0)]
    [InlineData(1, 0, 0)]
    [InlineData(1, 101, 0)]
    [InlineData(1, 10, 201)]
    public void RejectsUnboundedPagingAndSearch(int page, int limit, int searchLength) {
        var validator = new GetFavoriteRecipePageQueryValidator();
        Assert.False(validator.Validate(new GetFavoriteRecipePageQuery(Guid.NewGuid(), page, limit, new string('a', searchLength))).IsValid);
    }

    [Fact]
    public void AcceptsBoundedRequest() {
        Assert.True(new GetFavoriteRecipePageQueryValidator().Validate(new GetFavoriteRecipePageQuery(Guid.NewGuid(), 1, 10, "rice")).IsValid);
    }
}
