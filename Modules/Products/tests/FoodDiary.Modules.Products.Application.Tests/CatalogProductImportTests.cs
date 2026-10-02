using FoodDiary.Mediator;
using FoodDiary.Modules.Products.Application.Abstractions.Common;
using FoodDiary.Modules.Products.Application.Commands.CreateProduct;
using FoodDiary.Modules.Products.Application.Commands.ImportCatalogProduct;
using FoodDiary.Modules.Products.Application.Models;
using FoodDiary.Modules.Products.Application.Mappings;
using FoodDiary.Modules.Products.Domain.Entities;
using FoodDiary.Modules.Products.Domain.Contracts.Enums;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;
using ResultAssert = FoodDiary.Testing.Assertions.ResultAssert;

namespace FoodDiary.Modules.Products.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class CatalogProductImportTests {
    [Fact]
    public async Task Import_PreservesSourceIdAndUsesAuthenticatedOwner() {
        CatalogProductModel item = ValidProduct();
        var ownerId = Guid.NewGuid();
        ISender sender = Substitute.For<ISender>();
        var product = Product.Create(new UserId(ownerId), item.Name, MeasurementUnit.G, baseAmount: 100,
            defaultPortionAmount: 100, caloriesPerBase: 350, proteinsPerBase: 12, fatsPerBase: 6, carbsPerBase: 60,
            fiberPerBase: 5, alcoholPerBase: 0, importId: new FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids.ProductId(item.Id));
        sender.Send(Arg.Any<CreateProductCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success(product.ToModel(isOwnedByCurrentUser: true)));
        var handler = new ImportCatalogProductCommandHandler(Substitute.For<IProductCatalogIdReadService>(), new CreateProductCommandValidator(), sender,
            new ImmediateTransactionRunner());

        CatalogProductImportResult result = ResultAssert.Success(await handler.Handle(new ImportCatalogProductCommand(ownerId, item, Preview: false), CancellationToken.None));

        Assert.Multiple(() => Assert.Equal("imported", result.Status), () => Assert.Equal(item.Id, product.Id.Value));
        await sender.Received(1).Send(Arg.Is<CreateProductCommand>(create => create.CatalogImportId == item.Id && create.UserId == ownerId && create.Visibility == "Public" && create.Comment == null), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null, "ready")]
    [InlineData(true, "skipped")]
    [InlineData(false, "invalid")]
    public async Task Preview_DistinguishesNewPublicAndPrivateIds(bool? isPublic, string expectedStatus) {
        CatalogProductModel item = ValidProduct();
        IProductCatalogIdReadService repository = Substitute.For<IProductCatalogIdReadService>();
        repository.CatalogIdIsPublicAsync(item.Id, Arg.Any<CancellationToken>()).Returns(isPublic);
        ISender sender = Substitute.For<ISender>();
        IProductMutationTransactionRunner runner = Substitute.For<IProductMutationTransactionRunner>();
        var handler = new ImportCatalogProductCommandHandler(repository, new CreateProductCommandValidator(), sender, runner);

        CatalogProductImportResult result = ResultAssert.Success(await handler.Handle(new ImportCatalogProductCommand(Guid.NewGuid(), item, Preview: true), CancellationToken.None));

        Assert.Equal(expectedStatus, result.Status);
        Assert.Empty(sender.ReceivedCalls());
        Assert.Empty(runner.ReceivedCalls());
    }

    [Fact]
    public async Task Preview_RejectsInvalidNutritionWithoutDispatchingCreate() {
        CatalogProductModel item = ValidProduct() with { ProteinsPerBase = -1 };
        ISender sender = Substitute.For<ISender>();
        var handler = new ImportCatalogProductCommandHandler(Substitute.For<IProductCatalogIdReadService>(), new CreateProductCommandValidator(), sender,
            Substitute.For<IProductMutationTransactionRunner>());

        CatalogProductImportResult result = ResultAssert.Success(await handler.Handle(new ImportCatalogProductCommand(Guid.NewGuid(), item, Preview: true), CancellationToken.None));

        Assert.Equal("invalid", result.Status);
        Assert.Contains(result.Errors, error => error.StartsWith("ProteinsPerBase:", StringComparison.Ordinal));
        Assert.Empty(sender.ReceivedCalls());
    }

    [Fact]
    public async Task Preview_RejectsEmptyId() {
        var handler = new ImportCatalogProductCommandHandler(Substitute.For<IProductCatalogIdReadService>(), new CreateProductCommandValidator(), Substitute.For<ISender>(),
            Substitute.For<IProductMutationTransactionRunner>());
        CatalogProductImportResult result = ResultAssert.Success(await handler.Handle(new ImportCatalogProductCommand(Guid.NewGuid(), ValidProduct() with { Id = Guid.Empty }, Preview: true), CancellationToken.None));
        Assert.Equal("invalid", result.Status);
    }

    private static CatalogProductModel ValidProduct() => new(
        Guid.NewGuid(), "Овсяные хлопья", Barcode: null, Brand: null, "Unknown", Category: null, Description: null, ImageUrl: null,
        "G", BaseAmount: 100, DefaultPortionAmount: 100, CaloriesPerBase: 350, ProteinsPerBase: 12, FatsPerBase: 6,
        CarbsPerBase: 60, FiberPerBase: 5, AlcoholPerBase: 0);

    [ExcludeFromCodeCoverage]
    private sealed class ImmediateTransactionRunner : IProductMutationTransactionRunner {
        public Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default) =>
            operation(cancellationToken);
    }
}
