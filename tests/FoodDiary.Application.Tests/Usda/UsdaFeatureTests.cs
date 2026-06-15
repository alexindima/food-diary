using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Usda.Commands.LinkProductToUsdaFood;
using FoodDiary.Application.Usda.Commands.UnlinkProductFromUsdaFood;
using FoodDiary.Application.Abstractions.Usda.Common;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Usda;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;

namespace FoodDiary.Application.Tests.Usda;

[ExcludeFromCodeCoverage]
public class UsdaFeatureTests {
    [Fact]
    public async Task LinkProductToUsdaFood_WithValidData_Succeeds() {
        var userId = UserId.New();
        var product = Product.Create(userId, "Chicken", MeasurementUnit.G, 100, defaultPortionAmount: null, 165, 31, 3.6, 0, 0, 0);
        var usdaFood = new UsdaFood { FdcId = 171077, Description = "Chicken, breast" };
        IProductRepository productRepo = CreateProductRepository(product);
        IUsdaFoodRepository usdaRepo = CreateUsdaFoodRepository(usdaFood);

        var handler = new LinkProductToUsdaFoodCommandHandler(productRepo, usdaRepo);
        Result result = await handler.Handle(
            new LinkProductToUsdaFoodCommand(userId.Value, product.Id.Value, 171077),
            CancellationToken.None);

        ResultAssert.Success(result);
        await productRepo.Received(1).GetByIdForUpdateAsync(
            product.Id,
            userId,
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>());
        await productRepo.Received(1).UpdateAsync(product, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LinkProductToUsdaFood_WhenProductNotFound_ReturnsFailure() {
        var handler = new LinkProductToUsdaFoodCommandHandler(
            CreateProductRepository(product: null), CreateUsdaFoodRepository(food: null));

        Result result = await handler.Handle(
            new LinkProductToUsdaFoodCommand(Guid.NewGuid(), Guid.NewGuid(), 171077),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Contains("NotAccessible", result.Error.Code, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LinkProductToUsdaFood_WhenUsdaFoodNotFound_ReturnsFailure() {
        var userId = UserId.New();
        var product = Product.Create(userId, "Chicken", MeasurementUnit.G, 100, defaultPortionAmount: null, 165, 31, 3.6, 0, 0, 0);
        var handler = new LinkProductToUsdaFoodCommandHandler(
            CreateProductRepository(product), CreateUsdaFoodRepository(food: null));

        Result result = await handler.Handle(
            new LinkProductToUsdaFoodCommand(userId.Value, product.Id.Value, 999999),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Contains("FoodNotFound", result.Error.Code, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UnlinkProductFromUsdaFood_WithValidData_Succeeds() {
        var userId = UserId.New();
        var product = Product.Create(userId, "Chicken", MeasurementUnit.G, 100, defaultPortionAmount: null, 165, 31, 3.6, 0, 0, 0);
        IProductRepository productRepo = CreateProductRepository(product);

        var handler = new UnlinkProductFromUsdaFoodCommandHandler(productRepo);
        Result result = await handler.Handle(
            new UnlinkProductFromUsdaFoodCommand(userId.Value, product.Id.Value),
            CancellationToken.None);

        ResultAssert.Success(result);
        await productRepo.Received(1).GetByIdForUpdateAsync(
            product.Id,
            userId,
            Arg.Any<bool>(),
            Arg.Any<CancellationToken>());
        await productRepo.Received(1).UpdateAsync(product, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UnlinkProductFromUsdaFood_WhenProductNotFound_ReturnsFailure() {
        var handler = new UnlinkProductFromUsdaFoodCommandHandler(CreateProductRepository(product: null));

        Result result = await handler.Handle(
            new UnlinkProductFromUsdaFoodCommand(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task LinkProductToUsdaFood_WithNullUserId_ReturnsFailure() {
        var handler = new LinkProductToUsdaFoodCommandHandler(
            CreateProductRepository(product: null), CreateUsdaFoodRepository(food: null));

        Result result = await handler.Handle(
            new LinkProductToUsdaFoodCommand(UserId: null, Guid.NewGuid(), 1), CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task UnlinkProductFromUsdaFood_WithNullUserId_ReturnsFailure() {
        var handler = new UnlinkProductFromUsdaFoodCommandHandler(CreateProductRepository(product: null));

        Result result = await handler.Handle(
            new UnlinkProductFromUsdaFoodCommand(UserId: null, Guid.NewGuid()), CancellationToken.None);

        ResultAssert.Failure(result);
    }

    private static IProductRepository CreateProductRepository(Product? product) {
        IProductRepository repository = Substitute.For<IProductRepository>();
        repository
            .GetByIdAsync(Arg.Any<ProductId>(), Arg.Any<UserId>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(product));
        repository
            .GetByIdForUpdateAsync(Arg.Any<ProductId>(), Arg.Any<UserId>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(product));
        repository
            .UpdateAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        return repository;
    }

    private static IUsdaFoodRepository CreateUsdaFoodRepository(UsdaFood? food) {
        IUsdaFoodRepository repository = Substitute.For<IUsdaFoodRepository>();
        repository
            .GetByFdcIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(food));
        return repository;
    }
}
