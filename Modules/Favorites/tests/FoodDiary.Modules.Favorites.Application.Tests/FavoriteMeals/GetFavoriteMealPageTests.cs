using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteMeals.Common;
using FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteMeals.Models;
using FoodDiary.Modules.Favorites.Application.FavoriteMeals.Queries.GetFavoriteMealPage;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Favorites.Application.Tests.FavoriteMeals;

[ExcludeFromCodeCoverage]
public sealed class GetFavoriteMealPageTests {
    [Fact]
    public async Task ResolvesOwnerAndForwardsPageSearchAndCancellationAsync() {
        var owner = UserId.New();
        using var cancellation = new CancellationTokenSource();
        ICurrentUserAccessService access = Substitute.For<ICurrentUserAccessService>();
        access.EnsureCanAccessAsync(owner, cancellation.Token).Returns((Error?)null);
        IFavoriteMealQuery repository = Substitute.For<IFavoriteMealQuery>();
        var item = new FavoriteMealReadModel(Guid.NewGuid(), Guid.NewGuid(), "Lunch", DateTime.UtcNow, DateTime.UtcNow, MealType: null, 500, 20, 10, 30, 1) {
            AiImageUrls = ["https://example.com/rice.jpg", "https://example.com/ai.jpg"],
            ItemImageUrls = ["https://example.com/rice.jpg"], ImageUrl = "https://example.com/meal.jpg",
            TotalFiber = 7.5,
            ItemNames = ["Rice"],
            AiItemNames = ["Rice", "Coffee"],
        };
        repository.GetPageReadModelsAsync(owner, 2, 10, "rice", cancellation.Token).Returns((new[] { item }, 23));
        var handler = new GetFavoriteMealPageQueryHandler(repository, access);
        FoodDiary.Application.Abstractions.Common.Models.PagedResponse<FoodDiary.Modules.Favorites.Contracts.FavoriteMeals.Models.FavoriteMealModel> result = ResultAssert.Success(await handler.Handle(new GetFavoriteMealPageQuery(owner.Value, 2, 10, " rice "), cancellation.Token));
        Assert.Multiple(
            () => Assert.Equal(new[] { "https://example.com/rice.jpg", "https://example.com/ai.jpg" }, result.Data.Single().ItemImageUrls),
            () => Assert.Equal("https://example.com/meal.jpg", result.Data.Single().ImageUrl),
            () => Assert.Equal(7.5, result.Data.Single().TotalFiber),
            () => Assert.Equal(23, result.TotalItems),
            () => Assert.Equal(3, result.TotalPages),
            () => Assert.Equal(2, result.Page),
            () => Assert.Equal(new[] { "Rice", "Coffee" }, result.Data.Single().ItemNames));
        await access.Received(1).EnsureCanAccessAsync(owner, cancellation.Token);
    }

    [Fact]
    public async Task DeniedOwnerDoesNotReadFavoritesAsync() {
        var owner = UserId.New();
        ICurrentUserAccessService access = Substitute.For<ICurrentUserAccessService>();
        access.EnsureCanAccessAsync(owner, Arg.Any<CancellationToken>()).Returns(AuthenticationErrors.InvalidToken);
        IFavoriteMealQuery repository = Substitute.For<IFavoriteMealQuery>();
        var handler = new GetFavoriteMealPageQueryHandler(repository, access);
        ResultAssert.Failure(await handler.Handle(new GetFavoriteMealPageQuery(owner.Value), CancellationToken.None));
        Assert.Empty(repository.ReceivedCalls());
    }

    [Theory]
    [InlineData(0, 10, 0)]
    [InlineData(10001, 10, 0)]
    [InlineData(1, 0, 0)]
    [InlineData(1, 101, 0)]
    [InlineData(1, 10, 201)]
    public void RejectsUnboundedPagingAndSearch(int page, int limit, int searchLength) {
        var validator = new GetFavoriteMealPageQueryValidator();
        Assert.False(validator.Validate(new GetFavoriteMealPageQuery(Guid.NewGuid(), page, limit, new string('a', searchLength))).IsValid);
    }

    [Fact]
    public void AcceptsBoundedRequest() {
        Assert.True(new GetFavoriteMealPageQueryValidator().Validate(new GetFavoriteMealPageQuery(Guid.NewGuid(), 1, 10, "rice")).IsValid);
    }
}
