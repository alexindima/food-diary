using FoodDiary.Application.Abstractions.Products.Common;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence.Usda;

namespace FoodDiary.Infrastructure.Tests.Persistence;

[ExcludeFromCodeCoverage]
public sealed class UsdaProductLinkRepositoryTests {
    [Fact]
    public async Task GetForLinkUpdateAsync_DelegatesToProductRepositoryForPrivateUpdate() {
        Product product = CreateProduct();
        IProductWriteRepository productRepository = Substitute.For<IProductWriteRepository>();
        productRepository
            .GetByIdForUpdateAsync(product.Id, product.UserId, includePublic: false, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Product?>(product));
        var repository = new UsdaProductLinkRepository(productRepository);

        Product? result = await repository.GetForLinkUpdateAsync(product.Id, product.UserId, CancellationToken.None);

        Assert.Same(product, result);
    }

    [Fact]
    public async Task UpdateAsync_DelegatesToProductRepository() {
        Product product = CreateProduct();
        IProductWriteRepository productRepository = Substitute.For<IProductWriteRepository>();
        var repository = new UsdaProductLinkRepository(productRepository);

        await repository.UpdateAsync(product, CancellationToken.None);

        await productRepository.Received(1).UpdateAsync(product, CancellationToken.None);
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
