using FoodDiary.Results;
using FoodDiary.Application.Products.Commands.DeleteProduct;
using FoodDiary.Application.Products.Commands.DuplicateProduct;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Products.Models;

namespace FoodDiary.Application.Tests.Products;

public partial class ProductsFeatureTests {

    [Fact]
    public async Task DeleteProductCommandHandler_WithMissingUserId_ReturnsInvalidToken() {
        var handler = new DeleteProductCommandHandler(
            new NoopProductRepository(),
            new NoopProductRepository(),
            new RecordingCleanupService(),
            new StubUserRepository(User.Create("delete-product-invalid-token@example.com", "hash")));

        Result result = await handler.Handle(
            new DeleteProductCommand(UserId: null, ProductId.New().Value),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }


    [Fact]
    public async Task DeleteProductCommandHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("delete-product-deleted-user@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var repository = new NoopProductRepository();
        var handler = new DeleteProductCommandHandler(
            repository,
            repository,
            new RecordingCleanupService(),
            new StubUserRepository(user));

        Result result = await handler.Handle(
            new DeleteProductCommand(user.Id.Value, ProductId.New().Value),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }


    [Fact]
    public async Task DeleteProductCommandHandler_WhenProductMissing_ReturnsNotAccessible() {
        var user = User.Create("delete-product-missing@example.com", "hash");
        var repository = new NoopProductRepository();
        var handler = new DeleteProductCommandHandler(
            repository,
            repository,
            new RecordingCleanupService(),
            new StubUserRepository(user));

        Result result = await handler.Handle(
            new DeleteProductCommand(user.Id.Value, ProductId.New().Value),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Product.NotAccessible", result.Error.Code);
    }


    [Fact]
    public async Task DeleteProductCommandHandler_WhenProductIsUsed_ReturnsValidationFailure() {
        var user = User.Create("delete-product-used@example.com", "hash");
        var product = Product.Create(
            user.Id,
            name: "Apple",
            baseUnit: MeasurementUnit.G,
            baseAmount: 100,
            defaultPortionAmount: 100,
            caloriesPerBase: 52,
            proteinsPerBase: 0.3,
            fatsPerBase: 0.2,
            carbsPerBase: 14,
            fiberPerBase: 2.4,
            alcoholPerBase: 0,
            visibility: Visibility.Private);
        SetProductUsageCollections(product, mealItemsCount: 1, recipeIngredientsCount: 0);
        var repository = new SingleProductRepository(product);
        var cleanup = new RecordingCleanupService();
        var handler = new DeleteProductCommandHandler(repository, repository, cleanup, new StubUserRepository(user));

        Result result = await handler.Handle(new DeleteProductCommand(user.Id.Value, product.Id.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.False(repository.DeleteCalled);
        Assert.Empty(cleanup.RequestedAssetIds);
    }


    [Fact]
    public async Task DeleteProductCommandHandler_WhenCleanupFails_StillDeletesProductAndReturnsSuccess() {
        var userId = UserId.New();
        var assetId = ImageAssetId.New();
        var product = Product.Create(
            userId,
            name: "Apple",
            baseUnit: MeasurementUnit.G,
            baseAmount: 100,
            defaultPortionAmount: 100,
            caloriesPerBase: 52,
            proteinsPerBase: 0.3,
            fatsPerBase: 0.2,
            carbsPerBase: 14,
            fiberPerBase: 2.4,
            alcoholPerBase: 0,
            imageAssetId: assetId,
            visibility: Visibility.Private);

        var repository = new SingleProductRepository(product);
        var cleanup = new RecordingCleanupService("storage_error");
        var handler = new DeleteProductCommandHandler(repository, repository, cleanup, new StubUserRepository(User.Create("user@example.com", "hash")));

        Result result = await handler.Handle(new DeleteProductCommand(userId.Value, product.Id.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.True(repository.GetByIdForUpdateCalled);
        Assert.True(repository.DeleteCalled);
        Assert.Equal([assetId], cleanup.RequestedAssetIds);
    }


    [Fact]
    public async Task DeleteProductCommandHandler_WithEmptyProductId_ReturnsValidationFailure() {
        var handler = new DeleteProductCommandHandler(
            new NoopProductRepository(),
            new NoopProductRepository(),
            new RecordingCleanupService(),
            new StubUserRepository(User.Create("user@example.com", "hash")));

        Result result = await handler.Handle(
            new DeleteProductCommand(Guid.NewGuid(), Guid.Empty),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("ProductId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }


    [Fact]
    public async Task DuplicateProductCommandHandler_WithEmptyProductId_ReturnsValidationFailure() {
        var repository = new NoopProductRepository();
        var handler = new DuplicateProductCommandHandler(
            repository,
            repository,
            new StubUserRepository(User.Create("duplicate-product-empty-id@example.com", "hash")));

        Result<ProductModel> result = await handler.Handle(
            new DuplicateProductCommand(Guid.NewGuid(), Guid.Empty),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("ProductId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }


    [Fact]
    public async Task DuplicateProductCommandHandler_WithMissingUserId_ReturnsInvalidToken() {
        var repository = new NoopProductRepository();
        var handler = new DuplicateProductCommandHandler(
            repository,
            repository,
            new StubUserRepository(User.Create("duplicate-product-missing-user@example.com", "hash")));

        Result<ProductModel> result = await handler.Handle(
            new DuplicateProductCommand(UserId: null, ProductId.New().Value),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }


    [Fact]
    public async Task DuplicateProductCommandHandler_WhenOriginalMissing_ReturnsNotAccessible() {
        var user = User.Create("duplicate-product-missing-original@example.com", "hash");
        var repository = new NoopProductRepository();
        var handler = new DuplicateProductCommandHandler(repository, repository, new StubUserRepository(user));

        Result<ProductModel> result = await handler.Handle(
            new DuplicateProductCommand(user.Id.Value, ProductId.New().Value),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Product.NotAccessible", result.Error.Code);
    }


    [Fact]
    public async Task DuplicateProductCommandHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("duplicate-product-deleted@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var repository = new NoopProductRepository();
        var handler = new DuplicateProductCommandHandler(repository, repository, new StubUserRepository(user));

        Result<ProductModel> result = await handler.Handle(
            new DuplicateProductCommand(user.Id.Value, ProductId.New().Value),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }


    [Fact]
    public async Task DuplicateProductCommandHandler_WithExistingProduct_CopiesFieldsAndClearsImageAsset() {
        var user = User.Create("duplicate-product@example.com", "hash");
        var original = Product.Create(
            user.Id,
            name: "Greek Yogurt",
            baseUnit: MeasurementUnit.G,
            baseAmount: 100,
            defaultPortionAmount: 150,
            caloriesPerBase: 73,
            proteinsPerBase: 9.5,
            fatsPerBase: 2.1,
            carbsPerBase: 3.8,
            fiberPerBase: 0,
            alcoholPerBase: 0,
            barcode: "4601234567890",
            brand: "MilkCo",
            category: "Dairy",
            description: "High protein yogurt",
            comment: "Original note",
            imageUrl: "https://cdn.test/yogurt.png",
            imageAssetId: ImageAssetId.New(),
            visibility: Visibility.Public);

        var repository = new SingleProductRepository(original);
        var handler = new DuplicateProductCommandHandler(repository, repository, new StubUserRepository(user));

        Result<ProductModel> result = await handler.Handle(new DuplicateProductCommand(user.Id.Value, original.Id.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.NotNull(repository.LastAddedProduct);
        Assert.NotEqual(original.Id, repository.LastAddedProduct.Id);
        Assert.Equal(original.Name, repository.LastAddedProduct.Name);
        Assert.Equal(original.Barcode, repository.LastAddedProduct.Barcode);
        Assert.Equal(original.ImageUrl, repository.LastAddedProduct.ImageUrl);
        Assert.Null(repository.LastAddedProduct.ImageAssetId);
        Assert.Equal(user.Id, repository.LastAddedProduct.UserId);
        Assert.True(result.Value.IsOwnedByCurrentUser);
    }

}
