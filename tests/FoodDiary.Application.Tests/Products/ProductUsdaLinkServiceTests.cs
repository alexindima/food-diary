using FoodDiary.Application.Abstractions.Products.Common;
using FoodDiary.Application.Products.Products.Services;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Products;

[ExcludeFromCodeCoverage]
public sealed class ProductUsdaLinkServiceTests {
    [Fact]
    public async Task LinkAsync_WhenProductIsAccessible_MutatesAndPersistsOwnedAggregate() {
        Product product = CreateProduct();
        IProductWriteRepository repository = CreateRepository(product);
        var service = new ProductUsdaLinkService(repository);

        bool linked = await service.LinkAsync(product.Id, product.UserId, 171077, CancellationToken.None);

        Assert.True(linked);
        Assert.Equal(171077, product.UsdaFdcId);
        await repository.Received(1).UpdateAsync(product, CancellationToken.None);
    }

    [Fact]
    public async Task UnlinkAsync_WhenProductIsMissing_DoesNotPersist() {
        IProductWriteRepository repository = CreateRepository(product: null);
        var service = new ProductUsdaLinkService(repository);

        bool unlinked = await service.UnlinkAsync(ProductId.New(), UserId.New(), CancellationToken.None);

        Assert.False(unlinked);
        await repository.DidNotReceive().UpdateAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IsAccessibleForUpdateAsync_ReturnsWhetherOwnedProductExists() {
        Product product = CreateProduct();

        bool accessible = await new ProductUsdaLinkService(CreateRepository(product))
            .IsAccessibleForUpdateAsync(product.Id, product.UserId, CancellationToken.None);
        bool missing = await new ProductUsdaLinkService(CreateRepository(product: null))
            .IsAccessibleForUpdateAsync(ProductId.New(), UserId.New(), CancellationToken.None);

        Assert.True(accessible);
        Assert.False(missing);
    }

    [Fact]
    public async Task LinkAsync_WhenProductIsMissing_DoesNotPersist() {
        IProductWriteRepository repository = CreateRepository(product: null);
        var service = new ProductUsdaLinkService(repository);

        bool linked = await service.LinkAsync(ProductId.New(), UserId.New(), 171077, CancellationToken.None);

        Assert.False(linked);
        await repository.DidNotReceive().UpdateAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UnlinkAsync_WhenProductIsAccessible_RemovesLinkAndPersists() {
        Product product = CreateProduct();
        product.LinkToUsdaFood(171077);
        IProductWriteRepository repository = CreateRepository(product);
        var service = new ProductUsdaLinkService(repository);

        bool unlinked = await service.UnlinkAsync(product.Id, product.UserId, CancellationToken.None);

        Assert.True(unlinked);
        Assert.Null(product.UsdaFdcId);
        await repository.Received(1).UpdateAsync(product, CancellationToken.None);
    }

    private static IProductWriteRepository CreateRepository(Product? product) {
        IProductWriteRepository repository = Substitute.For<IProductWriteRepository>();
        repository
            .GetByIdForUpdateAsync(Arg.Any<ProductId>(), Arg.Any<UserId>(), includePublic: false, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(product));
        return repository;
    }

    private static Product CreateProduct() =>
        Product.Create(
            UserId.New(),
            "USDA linked product",
            MeasurementUnit.G,
            baseAmount: 100,
            defaultPortionAmount: 100,
            caloriesPerBase: 80,
            proteinsPerBase: 5,
            fatsPerBase: 2,
            carbsPerBase: 10,
            fiberPerBase: 1,
            alcoholPerBase: 0);
}
