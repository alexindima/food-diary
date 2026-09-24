using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteProducts.Common;
using FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteProducts.Models;
using FoodDiary.Modules.Favorites.Application.FavoriteProducts.Queries.GetFavoriteProductPage;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Favorites.Application.Tests.FavoriteProducts;

[ExcludeFromCodeCoverage]
public sealed class GetFavoriteProductPageTests {
    [Fact]
    public async Task ResolvesOwnerAndForwardsPageSearchAndCancellationAsync() {
        var owner = UserId.New();
        using var cancellation = new CancellationTokenSource();
        ICurrentUserAccessService access = Substitute.For<ICurrentUserAccessService>();
        access.EnsureCanAccessAsync(owner, cancellation.Token).Returns((Error?)null);
        IFavoriteProductQuery repository = Substitute.For<IFavoriteProductQuery>();
        var item = new FavoriteProductReadModel(Guid.NewGuid(), Guid.NewGuid(), owner.Value, Name: null, DateTime.UtcNow, "Rice", Brand: null, Barcode: null, Comment: null, "https://example.com/rice.jpg", CaloriesPerBase: 100, ProteinsPerBase: 2, FatsPerBase: 1, CarbsPerBase: 20, FiberPerBase: 7.5, AlcoholPerBase: 0, FoodDiary.Modules.Products.Domain.Contracts.Enums.ProductType.Grain, FoodDiary.Modules.Products.Domain.Contracts.Enums.MeasurementUnit.G, PreferredPortionAmount: 150, DefaultPortionAmount: 100, owner.Value);
        repository.GetPageReadModelsAsync(owner, 2, 10, "rice", cancellation.Token).Returns((new[] { item }, 23));
        var handler = new GetFavoriteProductPageQueryHandler(repository, access);
        FoodDiary.Application.Abstractions.Common.Models.PagedResponse<FoodDiary.Modules.Favorites.Contracts.FavoriteProducts.Models.FavoriteProductModel> result = ResultAssert.Success(await handler.Handle(new GetFavoriteProductPageQuery(owner.Value, 2, 10, " rice "), cancellation.Token));
        Assert.Multiple(
            () => Assert.Equal("https://example.com/rice.jpg", result.Data.Single().ImageUrl),
            () => Assert.Equal(7.5, result.Data.Single().FiberPerBase),
            () => Assert.Equal(23, result.TotalItems),
            () => Assert.Equal(3, result.TotalPages),
            () => Assert.Equal(2, result.Page));
        await access.Received(1).EnsureCanAccessAsync(owner, cancellation.Token);
    }

    [Fact]
    public async Task DeniedOwnerDoesNotReadFavoritesAsync() {
        var owner = UserId.New();
        ICurrentUserAccessService access = Substitute.For<ICurrentUserAccessService>();
        access.EnsureCanAccessAsync(owner, Arg.Any<CancellationToken>()).Returns(AuthenticationErrors.InvalidToken);
        IFavoriteProductQuery repository = Substitute.For<IFavoriteProductQuery>();
        var handler = new GetFavoriteProductPageQueryHandler(repository, access);
        ResultAssert.Failure(await handler.Handle(new GetFavoriteProductPageQuery(owner.Value), CancellationToken.None));
        Assert.Empty(repository.ReceivedCalls());
    }

    [Theory]
    [InlineData(0, 10, 0)]
    [InlineData(10001, 10, 0)]
    [InlineData(1, 0, 0)]
    [InlineData(1, 101, 0)]
    [InlineData(1, 10, 201)]
    public void RejectsUnboundedPagingAndSearch(int page, int limit, int searchLength) {
        var validator = new GetFavoriteProductPageQueryValidator();
        Assert.False(validator.Validate(new GetFavoriteProductPageQuery(Guid.NewGuid(), page, limit, new string('a', searchLength))).IsValid);
    }

    [Fact]
    public void AcceptsBoundedRequest() {
        Assert.True(new GetFavoriteProductPageQueryValidator().Validate(new GetFavoriteProductPageQuery(Guid.NewGuid(), 1, 10, "rice")).IsValid);
    }
}
