using FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteProducts.Common;
using FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteProducts.Models;
using FoodDiary.Modules.Favorites.Application.FavoriteProducts.Queries.ReadFavoriteProductOverview;
using FoodDiary.Modules.Favorites.Contracts.FavoriteProducts.Queries.ReadFavoriteProductOverview;
using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Favorites.Application.Tests.FavoriteProducts;

[ExcludeFromCodeCoverage]
public sealed class ReadFavoriteProductOverviewTests {
    [Theory]
    [InlineData(0, 0)]
    [InlineData(-1, 0)]
    [InlineData(100, 50)]
    public async Task ReadsCountAndOnlyRequestedProductStatesAsync(int requestedLimit, int expectedLimit) {
        var owner = UserId.New();
        ProductId[] ids = [ProductId.New()];
        using var cancellation = new CancellationTokenSource();
        IFavoriteProductQuery queries = Substitute.For<IFavoriteProductQuery>();
        FavoriteProductReadModel[] preview = [];
        var item = new FavoriteProductReadModel(Guid.NewGuid(), ids[0].Value, owner.Value, Name: null, DateTime.UtcNow, "Rice", Brand: null, Barcode: null, Comment: null, "https://example.com/rice.jpg", CaloriesPerBase: 100, ProteinsPerBase: 2, FatsPerBase: 1, CarbsPerBase: 20, FiberPerBase: 7.5, AlcoholPerBase: 0, FoodDiary.Modules.Products.Domain.Contracts.Enums.ProductType.Grain, FoodDiary.Modules.Products.Domain.Contracts.Enums.MeasurementUnit.G, PreferredPortionAmount: 150, DefaultPortionAmount: 100, owner.Value);
        queries.GetPageReadModelsAsync(owner, 1, expectedLimit, search: null, cancellation.Token).Returns((preview, 1000));
        queries.GetByProductIdsReadModelsAsync(owner, ids, cancellation.Token).Returns(new[] { item });
        var handler = new ReadFavoriteProductOverviewQueryHandler(queries);

        FavoriteProductOverviewModel result = await handler.Handle(new ReadFavoriteProductOverviewQuery(owner, ids, requestedLimit), cancellation.Token);

        Assert.Multiple(
            () => Assert.Equal(1000, result.Total),
            () => Assert.Empty(result.Preview),
            () => Assert.Equal(ids[0].Value, Assert.Single(result.Items).ProductId));
        await queries.Received(1).GetPageReadModelsAsync(owner, 1, expectedLimit, search: null, cancellation.Token);
        await queries.Received(1).GetByProductIdsReadModelsAsync(owner, ids, cancellation.Token);
        await queries.DidNotReceive().GetAllReadModelsAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>());
    }
}
