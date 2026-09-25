using FoodDiary.Modules.Images.Service.Contracts.Common;
using FoodDiary.Modules.Images.Service.Contracts.Models;
using FoodDiary.Results;
using FoodDiary.Modules.Products.Application.Models;
using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Images.Application.Abstractions.Common;
using FoodDiary.Modules.Products.Application.Commands.CreateProduct;
using FoodDiary.Modules.Products.Application.Commands.DeleteProduct;
using FoodDiary.Modules.Products.Application.Commands.DuplicateProduct;
using FoodDiary.Modules.Products.Application.Commands.UpdateProduct;
using FoodDiary.Modules.Products.Domain.Entities;
using FoodDiary.Modules.Products.Domain.Contracts.Enums;
using FoodDiary.Modules.Users.Domain.Entities;

namespace FoodDiary.Modules.Products.Application.Tests;

public partial class ProductsFeatureTests {
    [Theory]
    [InlineData("empty")]
    [InlineData("duplicate")]
    [InlineData("too-many")]
    public async Task Gallery_InvalidSelectionDoesNotCreateOrUpdate(string selection) {
        var user = User.Create("gallery@example.test", "hash");
        var id = Guid.NewGuid();
        Guid[] ids = selection switch {
            "empty" => [Guid.Empty],
            "duplicate" => [id, id],
            _ => [.. Enumerable.Range(0, 6).Select(_ => Guid.NewGuid())],
        };
        var repository = new NoopProductRepository();
        var access = new RecordingImageAssetAccessService();
        var create = new CreateProductCommandHandler(repository, new StubUserRepository(user), access);
        var update = new UpdateProductCommandHandler(repository, repository, new RecordingCleanupService(), new StubUserRepository(user), access, ProductTransactionRunner);

        Result<ProductModel> created = await create.Handle(CreateProductCommand(user.Id.Value) with { ImageAssetIds = ids }, CancellationToken.None);
        Result<ProductModel> updated = await update.Handle(CreateUpdateProductCommand(user.Id.Value, Guid.NewGuid()) with { ImageAssetIds = ids }, CancellationToken.None);

        ResultAssert.Failure(created);
        ResultAssert.Failure(updated);
        Assert.Multiple(
            () => Assert.Equal("Validation.Invalid", created.Error.Code),
            () => Assert.Equal("Validation.Invalid", updated.Error.Code),
            () => Assert.Null(repository.LastAddedProduct),
            () => Assert.All(access.RequestedAssetIds, id => Assert.Null(id)));
    }

    [Fact]
    public async Task UpdateGallery_WhenAccessDenied_PreservesExistingImages() {
        var user = User.Create("gallery@example.test", "hash");
        var product = Product.Create(user.Id, "Apple", MeasurementUnit.G, 100, 100, 52, 0, 0, 14, 0, 0);
        var image = new ProductImage(ImageAssetId.New(), "https://example.test/old.jpg", 0);
        product.ReplaceImages([image]);
        var repository = new SingleProductRepository(product);
        var cleanup = new RecordingCleanupService();
        var deniedId = ImageAssetId.New();
        IImageAssetAccessService access = Substitute.For<IImageAssetAccessService>();
        access.ResolveOptionalAsync(assetId: null, user.Id, Arg.Any<CancellationToken>())
            .Returns(Result.Success<ImageAssetReadModel?>(value: null));
        access.ResolveOptionalAsync(deniedId, user.Id, Arg.Any<CancellationToken>())
            .Returns(Result.Failure<ImageAssetReadModel?>(ImageErrors.Forbidden()));
        var handler = new UpdateProductCommandHandler(repository, repository, cleanup, new StubUserRepository(user), access, ProductTransactionRunner);

        Result<ProductModel> result = await handler.Handle(CreateUpdateProductCommand(user.Id.Value, product.Id.Value) with { ImageAssetIds = [deniedId.Value] }, CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Multiple(
            () => Assert.Equal("Image.Forbidden", result.Error.Code),
            () => Assert.Equal(image.ImageAssetId, Assert.Single(product.Images).ImageAssetId),
            () => Assert.Equal(image.ImageUrl, Assert.Single(product.Images).ImageUrl),
            () => Assert.False(repository.UpdateCalled),
            () => Assert.Empty(cleanup.RequestedAssetIds));
    }

    [Fact]
    public async Task Gallery_ReplacementCleansOnlyRemovedAssetsAndDeletionCleansEachRemainingAssetOnce() {
        var user = User.Create("gallery@example.test", "hash");
        var product = Product.Create(user.Id, "Apple", MeasurementUnit.G, 100, 100, 52, 0, 0, 14, 0, 0);
        var first = new ProductImage(ImageAssetId.New(), "https://example.test/first.jpg", 0);
        var second = new ProductImage(ImageAssetId.New(), "https://example.test/second.jpg", 1);
        var third = ImageAssetId.New();
        product.ReplaceImages([first, second]);
        var repository = new SingleProductRepository(product);
        var cleanup = new RecordingCleanupService();
        RecordingImageAssetAccessService access = new RecordingImageAssetAccessService().WithAsset(second.ImageAssetId, second.ImageUrl).WithAsset(third, "https://example.test/third.jpg");
        var update = new UpdateProductCommandHandler(repository, repository, cleanup, new StubUserRepository(user), access, ProductTransactionRunner);

        Result<ProductModel> result = await update.Handle(CreateUpdateProductCommand(user.Id.Value, product.Id.Value) with { ImageAssetIds = [second.ImageAssetId.Value, third.Value] }, CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Multiple(
            () => Assert.Equal(new[] { second.ImageAssetId, third }, product.Images.OrderBy(image => image.Position).Select(image => image.ImageAssetId)),
            () => Assert.Equal(second.ImageAssetId, product.ImageAssetId),
            () => Assert.Equal(new[] { first.ImageAssetId }, cleanup.RequestedAssetIds),
            () => Assert.True(repository.UpdateCalled));

        var deleteCleanup = new RecordingCleanupService();
        var delete = new DeleteProductCommandHandler(repository, repository, deleteCleanup, new StubUserRepository(user), ProductTransactionRunner);
        ResultAssert.Success(await delete.Handle(new DeleteProductCommand(user.Id.Value, product.Id.Value), CancellationToken.None));
        Assert.True(repository.DeleteCalled);
        Assert.Equal(new[] { second.ImageAssetId, third }, deleteCleanup.RequestedAssetIds);
    }

    [Fact]
    public async Task DuplicateGallery_PreservesOwnedImageOrder() {
        var user = User.Create("gallery@example.test", "hash");
        var product = Product.Create(user.Id, "Apple", MeasurementUnit.G, 100, 100, 52, 0, 0, 14, 0, 0);
        var first = new ProductImage(ImageAssetId.New(), "https://example.test/first.jpg", 0);
        var second = new ProductImage(ImageAssetId.New(), "https://example.test/second.jpg", 1);
        product.ReplaceImages([second, first]);
        var repository = new SingleProductRepository(product);
        var handler = new DuplicateProductCommandHandler(repository, repository, new StubUserRepository(user));

        Result<ProductModel> result = await handler.Handle(new DuplicateProductCommand(user.Id.Value, product.Id.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Product duplicate = Assert.IsType<Product>(repository.LastAddedProduct);
        Assert.Multiple(
            () => Assert.NotEqual(product.Id, duplicate.Id),
            () => Assert.Equal(new[] { second.ImageAssetId, first.ImageAssetId }, duplicate.Images.OrderBy(image => image.Position).Select(image => image.ImageAssetId)),
            () => Assert.Equal(second.ImageAssetId, duplicate.ImageAssetId),
            () => Assert.Equal(new[] { second.ImageUrl, first.ImageUrl }, result.Value.Images.Select(image => image.ImageUrl), StringComparer.Ordinal));
    }
}
