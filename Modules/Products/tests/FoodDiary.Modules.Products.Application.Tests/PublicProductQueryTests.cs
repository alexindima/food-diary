using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Products.Application.Queries.GetPublicProduct;
using FoodDiary.Modules.Products.Contracts.Common;
using FoodDiary.Modules.Products.Contracts.Models;
using FoodDiary.Modules.Products.Domain.Contracts.Enums;
using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Products.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class PublicProductQueryTests {
    [Theory]
    [InlineData(Visibility.Private, false)]
    [InlineData(Visibility.Public, true)]
    public async Task OnlyPublicProductIsReturned(Visibility visibility, bool expectedSuccess) {
        var item = new ProductOverviewReadItem(ProductId.New(), UserId.New(), Barcode: null, "Apple", Brand: null,
            ProductType.Unknown, Category: null, Description: "Public description", Comment: "private note", ImageUrl: "cover.webp", ImageAssetId: null,
            MeasurementUnit.G, 100, 100, 52, 0, 0, 14, 0, 0, 0, visibility, DateTime.UtcNow, IsOwnedByCurrentUser: false,
            80, "green", UsdaFdcId: null) { Images = [new ProductImageReadItem(ImageAssetId: null, "cover.webp"), new ProductImageReadItem(ImageAssetId: null, "back.webp")], };
        IProductOverviewReadService read = Substitute.For<IProductOverviewReadService>();
        read.GetByIdsWithUsageAsync(Arg.Any<IEnumerable<ProductId>>(), UserId.Empty, includePublic: true, Arg.Any<CancellationToken>())
            .Returns(new Dictionary<ProductId, ProductOverviewReadItem> { [item.Id] = item });
        FoodDiary.Results.Result<FoodDiary.Modules.Products.Application.Models.PublicProductModel> result = await new GetPublicProductQueryHandler(read).Handle(new GetPublicProductQuery(item.Id.Value), CancellationToken.None);
        Assert.Equal(expectedSuccess, result.IsSuccess);
        if (result.IsSuccess) {
            Assert.Equal("Apple", result.Value.Name);
            Assert.Equal(100, result.Value.BaseAmount);
            Assert.Equal(52, result.Value.Calories);
            Assert.Equal("Public description", result.Value.Description);
            Assert.Equal(new[] { "cover.webp", "back.webp" }, result.Value.Images, StringComparer.Ordinal);
        }
    }
}
