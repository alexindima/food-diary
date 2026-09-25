using FoodDiary.Modules.Products.Contracts.Common;
using FoodDiary.Modules.Products.Contracts.Models;
using FoodDiary.Modules.Products.Domain.Contracts.Enums;
using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Favorites.Contracts.FavoriteProducts.Models;
using FoodDiary.Domain.Primitives;
using FoodDiary.Results;

namespace FoodDiary.Modules.Products.Infrastructure.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class FavoriteProductSourceReadServiceTests {
    [Fact]
    public async Task GetAccessibleAsync_PreservesGalleryOrderFromAuthorizedLookup() {
        var owner = UserId.New();
        var item = new ProductOverviewReadItem(ProductId.New(), owner, Barcode: null, "Apple", Brand: null,
            ProductType.Fruit, Category: null, Description: null, Comment: null, ImageUrl: null, ImageAssetId: null,
            MeasurementUnit.G, 100, 100, 52, 0, 0, 14, 0, 0, 0, Visibility.Private, DateTime.UtcNow,
            IsOwnedByCurrentUser: true, 80, "green", UsdaFdcId: null) {
            Images = [new(Guid.NewGuid(), "https://example.test/front.jpg"), new(Guid.NewGuid(), "https://example.test/back.jpg")],
        };
        using var cancellation = new CancellationTokenSource();
        var lookup = new GalleryLookup(item, cancellation.Token);
        var reader = new FavoriteProductSourceReadService(lookup);

        Result<FavoriteProductSourceModel> result = await reader.GetAccessibleAsync(item.Id, owner, cancellation.Token);

        FavoriteProductSourceModel model = FoodDiary.Testing.Assertions.ResultAssert.Success(result);
        Assert.Multiple(
            () => Assert.Equal(item.Name, model.Name),
            () => Assert.Equal(item.Images.Select(image => image.ImageUrl), model.ImageUrls, StringComparer.Ordinal));
    }

    [ExcludeFromCodeCoverage]
    private sealed class GalleryLookup(ProductOverviewReadItem item, CancellationToken expectedToken) : IProductLookupService {
        public Task<IReadOnlyDictionary<ProductId, ProductOverviewReadItem>> GetAccessibleByIdsAsync(
            IEnumerable<ProductId> ids, UserId userId, CancellationToken cancellationToken = default) {
            Assert.Multiple(
                () => Assert.Equal(item.Id, Assert.Single(ids)),
                () => Assert.Equal(item.UserId, userId),
                () => Assert.Equal(expectedToken, cancellationToken));
            return Task.FromResult<IReadOnlyDictionary<ProductId, ProductOverviewReadItem>>(new Dictionary<ProductId, ProductOverviewReadItem> { [item.Id] = item });
        }
    }
}
