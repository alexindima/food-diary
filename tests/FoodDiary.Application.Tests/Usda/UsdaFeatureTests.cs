using FoodDiary.Application.Usda.Commands.LinkProductToUsdaFood;
using FoodDiary.Application.Usda.Commands.UnlinkProductFromUsdaFood;
using FoodDiary.Application.Abstractions.Usda.Common;
using FoodDiary.Application.Abstractions.Products.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Usda;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests.Usda;

[ExcludeFromCodeCoverage]
public class UsdaFeatureTests {
    [Fact]
    public async Task LinkProductToUsdaFood_WithValidData_Succeeds() {
        var userId = UserId.New();
        var product = Product.Create(userId, "Chicken", MeasurementUnit.G, 100, defaultPortionAmount: null, 165, 31, 3.6, 0, 0, 0);
        var usdaFood = new UsdaFood { FdcId = 171077, Description = "Chicken, breast" };
        IProductUsdaLinkService productLinkService = CreateProductLinkService(product);
        IUsdaFoodRepository usdaRepo = CreateUsdaFoodRepository(usdaFood);

        var handler = new LinkProductToUsdaFoodCommandHandler(productLinkService, usdaRepo, Substitute.For<ICurrentUserAccessService>());
        Result result = await handler.Handle(
            new LinkProductToUsdaFoodCommand(userId.Value, product.Id.Value, 171077),
            CancellationToken.None);

        ResultAssert.Success(result);
        await productLinkService.Received(1).IsAccessibleForUpdateAsync(
            product.Id,
            userId,
            Arg.Any<CancellationToken>());
        await productLinkService.Received(1).LinkAsync(product.Id, userId, 171077, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LinkProductToUsdaFood_WhenProductNotFound_ReturnsFailure() {
        var handler = new LinkProductToUsdaFoodCommandHandler(
            CreateProductLinkService(product: null), CreateUsdaFoodRepository(food: null), Substitute.For<ICurrentUserAccessService>());

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
            CreateProductLinkService(product), CreateUsdaFoodRepository(food: null), Substitute.For<ICurrentUserAccessService>());

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
        IProductUsdaLinkService productLinkService = CreateProductLinkService(product);

        var handler = new UnlinkProductFromUsdaFoodCommandHandler(productLinkService, Substitute.For<ICurrentUserAccessService>());
        Result result = await handler.Handle(
            new UnlinkProductFromUsdaFoodCommand(userId.Value, product.Id.Value),
            CancellationToken.None);

        ResultAssert.Success(result);
        await productLinkService.Received(1).UnlinkAsync(product.Id, userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UnlinkProductFromUsdaFood_WhenProductNotFound_ReturnsFailure() {
        var handler = new UnlinkProductFromUsdaFoodCommandHandler(CreateProductLinkService(product: null), Substitute.For<ICurrentUserAccessService>());

        Result result = await handler.Handle(
            new UnlinkProductFromUsdaFoodCommand(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task LinkProductToUsdaFood_WithNullUserId_ReturnsFailure() {
        var handler = new LinkProductToUsdaFoodCommandHandler(
            CreateProductLinkService(product: null), CreateUsdaFoodRepository(food: null), Substitute.For<ICurrentUserAccessService>());

        Result result = await handler.Handle(
            new LinkProductToUsdaFoodCommand(UserId: null, Guid.NewGuid(), 1), CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task UnlinkProductFromUsdaFood_WithNullUserId_ReturnsFailure() {
        var handler = new UnlinkProductFromUsdaFoodCommandHandler(CreateProductLinkService(product: null), Substitute.For<ICurrentUserAccessService>());

        Result result = await handler.Handle(
            new UnlinkProductFromUsdaFoodCommand(UserId: null, Guid.NewGuid()), CancellationToken.None);

        ResultAssert.Failure(result);
    }

    private static IProductUsdaLinkService CreateProductLinkService(Product? product) {
        IProductUsdaLinkService service = Substitute.For<IProductUsdaLinkService>();
        service
            .IsAccessibleForUpdateAsync(Arg.Any<ProductId>(), Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(product is not null);
        service
            .LinkAsync(Arg.Any<ProductId>(), Arg.Any<UserId>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(product is not null);
        service
            .UnlinkAsync(Arg.Any<ProductId>(), Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(product is not null);
        return service;
    }

    private static IUsdaFoodRepository CreateUsdaFoodRepository(UsdaFood? food) {
        IUsdaFoodRepository repository = Substitute.For<IUsdaFoodRepository>();
        repository
            .GetByFdcIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(food));
        return repository;
    }
}
