using FoodDiary.Modules.Products.Application.Queries.ExportCatalogProducts;
using FoodDiary.Modules.Products.Contracts.Common;
using FoodDiary.Modules.Products.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Products.Application.Models;
using FoodDiary.Results;
using ResultAssert = FoodDiary.Testing.Assertions.ResultAssert;
using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Products.Domain.Contracts.Enums;
using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Products.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class ExportCatalogProductsQueryTests {
    [Fact]
    public async Task ExportAtLimitReadsBoundedPublicPagesAsync() {
        var item = new ProductOverviewReadItem(ProductId.New(), UserId.New(), Barcode: null, "Apple", Brand: null,
            ProductType.Unknown, Category: null, Description: null, Comment: "private note", ImageUrl: null, ImageAssetId: null,
            MeasurementUnit.G, 100, 100, 52, 0, 0, 14, 0, 0, 0, Visibility.Public, DateTime.UtcNow, IsOwnedByCurrentUser: false,
            80, "green", UsdaFdcId: null);
        IReadOnlyList<ProductOverviewReadItem> items = Enumerable.Repeat(item, 100).ToArray();
        IProductOverviewReadService read = Substitute.For<IProductOverviewReadService>();
        read.GetPagedAsync(UserId.Empty, includePublic: true, Arg.Any<int>(), 100, Arg.Any<ProductQueryFilters>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns((items, 5000));
        using var cancellation = new CancellationTokenSource();

        Result<IReadOnlyList<CatalogProductModel>> result = await new ExportCatalogProductsQueryHandler(read).Handle(new ExportCatalogProductsQuery(), cancellationToken: cancellation.Token);

        Assert.Equal(5000, ResultAssert.Success(result).Count);
        await read.Received(50).GetPagedAsync(UserId.Empty, includePublic: true, Arg.Any<int>(), 100,
            Arg.Is<ProductQueryFilters>(filter => filter.Search == null), cancellationToken: cancellation.Token);
        int[] pages = [.. read.ReceivedCalls().Select(call => (int)call.GetArguments()[2]!)];
        Assert.Equal(Enumerable.Range(1, 50), pages);
    }

    [Fact]
    public async Task ExportAboveLimitFailsWithoutContinuingAsync() {
        IProductOverviewReadService read = Substitute.For<IProductOverviewReadService>();
        read.GetPagedAsync(UserId.Empty, includePublic: true, 1, 100, Arg.Any<ProductQueryFilters>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<ProductOverviewReadItem>)[], 5001));

        Result<IReadOnlyList<CatalogProductModel>> result = await new ExportCatalogProductsQueryHandler(read).Handle(new ExportCatalogProductsQuery(), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Single(read.ReceivedCalls());
    }
}
