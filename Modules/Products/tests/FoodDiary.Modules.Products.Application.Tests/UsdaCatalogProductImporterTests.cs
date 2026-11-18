using FoodDiary.Modules.Products.Application.Abstractions.Common;
using FoodDiary.Modules.Products.Application.Commands.ImportUsdaCatalog;
using FoodDiary.Modules.Products.Domain.Entities;

namespace FoodDiary.Modules.Products.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class UsdaCatalogProductImporterTests {
    private const int FdcId = 168462;

    [Theory]
    [InlineData("Unrecognized")]
    [InlineData("Unknown")]
    public async Task FindNewAsync_RejectsInvalidProductTypeBeforeLookingUpIds(string productType) {
        IProductCatalogIdReadService catalog = Substitute.For<IProductCatalogIdReadService>();
        var importer = new UsdaCatalogProductImporter(catalog, Substitute.For<IProductWriteRepository>(),
            Substitute.For<IProductMutationTransactionRunner>());

        InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => importer.FindNewAsync([CreateProduct(productType)], CancellationToken.None));

        Assert.Equal("Invalid product type for USDA 168462: " + productType, error.Message);
        await catalog.DidNotReceive().FindUsdaCatalogMatchAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ImportAsync_DoesNotStageProductsWhenAnyStableIdCollides() {
        const int conflictingFdcId = FdcId + 1;
        IProductCatalogIdReadService catalog = Substitute.For<IProductCatalogIdReadService>();
        IProductWriteRepository products = Substitute.For<IProductWriteRepository>();
        IProductMutationTransactionRunner transaction = Substitute.For<IProductMutationTransactionRunner>();
        using var cancellation = new CancellationTokenSource();
        CancellationToken token = cancellation.Token;
        transaction.ExecuteAsync(Arg.Any<Func<CancellationToken, Task<IReadOnlySet<int>>>>(), token)
            .Returns(call => call.Arg<Func<CancellationToken, Task<IReadOnlySet<int>>>>()(token));
        catalog.FindUsdaCatalogMatchAsync(Guid.Parse("67fb0bd8-09f9-88f5-80a9-00000002920f"), conflictingFdcId, token)
            .Returns(returnThis: false);
        var importer = new UsdaCatalogProductImporter(catalog, products, transaction);

        InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => importer.ImportAsync([CreateProduct("Grain"), CreateProduct("Grain") with { FdcId = conflictingFdcId }], token));

        Assert.Equal("The catalog ID for USDA 168463 belongs to a different product.", error.Message);
        await products.DidNotReceive().AddAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
    }

    private static UsdaCatalogProduct CreateProduct(string productType) =>
        new(FdcId, "Гречка", "Buckwheat", productType, "Крупы", 343, 13.25, 3.4, 71.5, 10, 0);
}
