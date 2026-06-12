using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.FavoriteProducts.Common;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Application.Products.Commands.DeleteProduct;
using FoodDiary.Application.Products.Commands.CreateProduct;
using FoodDiary.Application.Products.Commands.DuplicateProduct;
using FoodDiary.Application.Products.Commands.UpdateProduct;
using FoodDiary.Application.Products.Mappings;
using FoodDiary.Application.Products.Queries.GetProductById;
using FoodDiary.Application.Products.Queries.GetProducts;
using FoodDiary.Application.Products.Queries.GetProductsOverview;
using FoodDiary.Application.Products.Queries.GetRecentProducts;
using FoodDiary.Application.Abstractions.RecentItems.Common;
using FoodDiary.Domain.Entities.FavoriteProducts;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Products.Models;
using FluentValidation.Results;
using FoodDiary.Application.Common.Models;

namespace FoodDiary.Application.Tests.Products;

[ExcludeFromCodeCoverage]
public class ProductsFeatureTests {
    [Fact]
    public void ProductMappings_ToModel_HidesOwnerCommentForNonOwnerAndPreservesFavoriteMetadata() {
        var favoriteProductId = FavoriteProductId.New();
        var product = Product.Create(
            UserId.New(),
            name: "Apple",
            baseUnit: MeasurementUnit.G,
            baseAmount: 100,
            defaultPortionAmount: 120,
            caloriesPerBase: 52,
            proteinsPerBase: 0.3,
            fatsPerBase: 0.2,
            carbsPerBase: 14,
            fiberPerBase: 2.4,
            alcoholPerBase: 0,
            comment: "Owner note",
            visibility: Visibility.Private);

        ProductModel nonOwnerModel = product.ToModel(
            usageCount: 7,
            isOwnedByCurrentUser: false,
            isFavorite: true,
            favoriteProductId: favoriteProductId.Value);
        ProductModel ownerModel = product.ToModel(isOwnedByCurrentUser: true);

        Assert.Null(nonOwnerModel.Comment);
        Assert.Equal("Owner note", ownerModel.Comment);
        Assert.Equal(7, nonOwnerModel.UsageCount);
        Assert.True(nonOwnerModel.IsFavorite);
        Assert.Equal(favoriteProductId.Value, nonOwnerModel.FavoriteProductId);
    }

    [Fact]
    public async Task GetProductsOverviewQueryValidator_WithEmptyUserId_Fails() {
        var validator = new GetProductsOverviewQueryValidator();
        var query = new GetProductsOverviewQuery(Guid.Empty, 1, 10, Search: null, IncludePublic: true);

        ValidationResult result = await validator.ValidateAsync(query);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task GetRecentProductsQueryValidator_WithValidUserId_Passes() {
        var validator = new GetRecentProductsQueryValidator();
        var query = new GetRecentProductsQuery(Guid.NewGuid(), 10, IncludePublic: true);

        ValidationResult result = await validator.ValidateAsync(query);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task GetProductsQueryHandler_WithMissingUserId_ReturnsInvalidToken() {
        var handler = new GetProductsQueryHandler(new NoopProductRepository(), new StubUserRepository(User.Create("user@example.com", "hash")));
        var query = new GetProductsQuery(UserId: null, 1, 10, Search: null, IncludePublic: true);

        Result<PagedResponse<ProductModel>> result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetProductsQueryHandler_ReturnsPagedProductsAndAppliesProductTypeFilter() {
        var user = User.Create("products-page@example.com", "hash");
        var owned = Product.Create(
            user.Id,
            name: "Owned Apple",
            baseUnit: MeasurementUnit.G,
            baseAmount: 100,
            defaultPortionAmount: 120,
            caloriesPerBase: 52,
            proteinsPerBase: 0.3,
            fatsPerBase: 0.2,
            carbsPerBase: 14,
            fiberPerBase: 2.4,
            alcoholPerBase: 0,
            productType: ProductType.Fruit,
            visibility: Visibility.Private);
        var supplement = Product.Create(
            user.Id,
            name: "Protein",
            baseUnit: MeasurementUnit.G,
            baseAmount: 100,
            defaultPortionAmount: 30,
            caloriesPerBase: 120,
            proteinsPerBase: 24,
            fatsPerBase: 2,
            carbsPerBase: 4,
            fiberPerBase: 0,
            alcoholPerBase: 0,
            productType: ProductType.Dairy,
            visibility: Visibility.Private);
        var repository = new OverviewProductRepository([(owned, 4), (supplement, 2)]);
        var handler = new GetProductsQueryHandler(repository, new StubUserRepository(user));

        Result<PagedResponse<ProductModel>> result = await handler.Handle(
            new GetProductsQuery(user.Id.Value, Page: 0, Limit: 0, Search: "ignored", IncludePublic: true, ProductTypes: ["fruit", "Fruit", "invalid"]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.Page);
        Assert.Equal(1, result.Value.Limit);
        ProductModel item = Assert.Single(result.Value.Data);
        Assert.Equal(owned.Id.Value, item.Id);
        Assert.Equal(4, item.UsageCount);
        Assert.True(item.IsOwnedByCurrentUser);
    }

    [Fact]
    public async Task CreateProductCommandHandler_WithMissingUserId_ReturnsInvalidToken() {
        var handler = new CreateProductCommandHandler(new NoopProductRepository(), new StubUserRepository(User.Create("user@example.com", "hash")), FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);
        var command = new CreateProductCommand(
            UserId: null,
            Barcode: null,
            Name: "Apple",
            Brand: null,
            ProductType: "Unknown",
            Category: null,
            Description: null,
            Comment: null,
            ImageUrl: null,
            ImageAssetId: null,
            BaseUnit: "G",
            BaseAmount: 100,
            DefaultPortionAmount: 100,
            CaloriesPerBase: 52,
            ProteinsPerBase: 0.3,
            FatsPerBase: 0.2,
            CarbsPerBase: 14,
            FiberPerBase: 2.4,
            AlcoholPerBase: 0,
            Visibility: "Private");

        Result<ProductModel> result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task CreateProductCommandHandler_WithEmptyImageAssetId_ReturnsValidationFailure() {
        var handler = new CreateProductCommandHandler(new NoopProductRepository(), new StubUserRepository(User.Create("user@example.com", "hash")), FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);
        var command = new CreateProductCommand(
            UserId: Guid.NewGuid(),
            Barcode: null,
            Name: "Apple",
            Brand: null,
            ProductType: "Unknown",
            Category: null,
            Description: null,
            Comment: null,
            ImageUrl: null,
            ImageAssetId: Guid.Empty,
            BaseUnit: "G",
            BaseAmount: 100,
            DefaultPortionAmount: 100,
            CaloriesPerBase: 52,
            ProteinsPerBase: 0.3,
            FatsPerBase: 0.2,
            CarbsPerBase: 14,
            FiberPerBase: 2.4,
            AlcoholPerBase: 0,
            Visibility: "Private");

        Result<ProductModel> result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("ImageAssetId", result.Error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateProductCommandHandler_WithInvalidProductType_ReturnsValidationFailure() {
        var user = User.Create("create-product-invalid-type@example.com", "hash");
        var repository = new NoopProductRepository();
        var handler = new CreateProductCommandHandler(repository, new StubUserRepository(user), FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        Result<ProductModel> result = await handler.Handle(
            new CreateProductCommand(
                UserId: user.Id.Value,
                Barcode: null,
                Name: "Apple",
                Brand: null,
                ProductType: "NotAProductType",
                Category: null,
                Description: null,
                Comment: null,
                ImageUrl: null,
                ImageAssetId: null,
                BaseUnit: "G",
                BaseAmount: 100,
                DefaultPortionAmount: 100,
                CaloriesPerBase: 52,
                ProteinsPerBase: 0.3,
                FatsPerBase: 0.2,
                CarbsPerBase: 14,
                FiberPerBase: 2.4,
                AlcoholPerBase: 0,
                Visibility: "Private"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Null(repository.LastAddedProduct);
    }

    [Fact]
    public async Task CreateProductCommandHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("create-product-deleted-user@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var repository = new NoopProductRepository();
        var handler = new CreateProductCommandHandler(repository, new StubUserRepository(user), FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        Result<ProductModel> result = await handler.Handle(
            CreateProductCommand(user.Id.Value),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
        Assert.Null(repository.LastAddedProduct);
    }

    [Fact]
    public async Task CreateProductCommandHandler_WithInvalidBaseUnit_ReturnsValidationFailure() {
        var user = User.Create("create-product-invalid-unit@example.com", "hash");
        var repository = new NoopProductRepository();
        var handler = new CreateProductCommandHandler(repository, new StubUserRepository(user), FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        Result<ProductModel> result = await handler.Handle(
            CreateProductCommand(user.Id.Value, baseUnit: "Cup"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Null(repository.LastAddedProduct);
    }

    [Fact]
    public async Task CreateProductCommandHandler_WithInvalidVisibility_ReturnsValidationFailure() {
        var user = User.Create("create-product-invalid-visibility@example.com", "hash");
        var repository = new NoopProductRepository();
        var handler = new CreateProductCommandHandler(repository, new StubUserRepository(user), FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        Result<ProductModel> result = await handler.Handle(
            CreateProductCommand(user.Id.Value, visibility: "Shared"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Null(repository.LastAddedProduct);
    }

    [Fact]
    public async Task CreateProductCommandHandler_WithUndefinedNumericProductType_ReturnsValidationFailure() {
        var user = User.Create("create-product-undefined-type@example.com", "hash");
        var repository = new NoopProductRepository();
        var handler = new CreateProductCommandHandler(repository, new StubUserRepository(user), FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        Result<ProductModel> result = await handler.Handle(
            CreateProductCommand(user.Id.Value, productType: "999"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Null(repository.LastAddedProduct);
    }

    [Fact]
    public async Task DeleteProductCommandHandler_WithMissingUserId_ReturnsInvalidToken() {
        var handler = new DeleteProductCommandHandler(
            new NoopProductRepository(),
            new RecordingCleanupService(),
            new StubUserRepository(User.Create("delete-product-invalid-token@example.com", "hash")));

        Result result = await handler.Handle(
            new DeleteProductCommand(UserId: null, ProductId.New().Value),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task DeleteProductCommandHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("delete-product-deleted-user@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var repository = new NoopProductRepository();
        var handler = new DeleteProductCommandHandler(
            repository,
            new RecordingCleanupService(),
            new StubUserRepository(user));

        Result result = await handler.Handle(
            new DeleteProductCommand(user.Id.Value, ProductId.New().Value),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task DeleteProductCommandHandler_WhenProductMissing_ReturnsNotAccessible() {
        var user = User.Create("delete-product-missing@example.com", "hash");
        var repository = new NoopProductRepository();
        var handler = new DeleteProductCommandHandler(
            repository,
            new RecordingCleanupService(),
            new StubUserRepository(user));

        Result result = await handler.Handle(
            new DeleteProductCommand(user.Id.Value, ProductId.New().Value),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Product.NotAccessible", result.Error.Code);
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
        var handler = new DeleteProductCommandHandler(repository, cleanup, new StubUserRepository(User.Create("user@example.com", "hash")));

        Result result = await handler.Handle(new DeleteProductCommand(userId.Value, product.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(repository.GetByIdForUpdateCalled);
        Assert.True(repository.DeleteCalled);
        Assert.Equal([assetId], cleanup.RequestedAssetIds);
    }

    [Fact]
    public async Task DeleteProductCommandHandler_WithEmptyProductId_ReturnsValidationFailure() {
        var handler = new DeleteProductCommandHandler(
            new NoopProductRepository(),
            new RecordingCleanupService(),
            new StubUserRepository(User.Create("user@example.com", "hash")));

        Result result = await handler.Handle(
            new DeleteProductCommand(Guid.NewGuid(), Guid.Empty),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("ProductId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetProductByIdQueryHandler_WithEmptyProductId_ReturnsValidationFailure() {
        var handler = new GetProductByIdQueryHandler(new NoopProductRepository(), new StubUserRepository(User.Create("user@example.com", "hash")));

        Result<ProductModel> result = await handler.Handle(
            new GetProductByIdQuery(Guid.NewGuid(), Guid.Empty),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("ProductId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetProductByIdQueryHandler_WithMissingUserId_ReturnsInvalidToken() {
        var handler = new GetProductByIdQueryHandler(
            new NoopProductRepository(),
            new StubUserRepository(User.Create("product-by-id-invalid-token@example.com", "hash")));

        Result<ProductModel> result = await handler.Handle(
            new GetProductByIdQuery(UserId: null, ProductId.New().Value),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetProductByIdQueryHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("product-by-id-deleted-user@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new GetProductByIdQueryHandler(
            new NoopProductRepository(),
            new StubUserRepository(user));

        Result<ProductModel> result = await handler.Handle(
            new GetProductByIdQuery(user.Id.Value, ProductId.New().Value),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task DuplicateProductCommandHandler_WithEmptyProductId_ReturnsValidationFailure() {
        var handler = new DuplicateProductCommandHandler(new NoopProductRepository());

        Result<ProductModel> result = await handler.Handle(
            new DuplicateProductCommand(Guid.NewGuid(), Guid.Empty),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("ProductId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DuplicateProductCommandHandler_WithMissingUserId_ReturnsInvalidToken() {
        var handler = new DuplicateProductCommandHandler(new NoopProductRepository());

        Result<ProductModel> result = await handler.Handle(
            new DuplicateProductCommand(UserId: null, ProductId.New().Value),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task DuplicateProductCommandHandler_WhenOriginalMissing_ReturnsNotFound() {
        var handler = new DuplicateProductCommandHandler(new NoopProductRepository());

        Result<ProductModel> result = await handler.Handle(
            new DuplicateProductCommand(Guid.NewGuid(), ProductId.New().Value),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Product.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task UpdateProductCommandHandler_WhenCleanupFails_StillReturnsSuccessAndUpdatesProduct() {
        var userId = UserId.New();
        var oldAssetId = ImageAssetId.New();
        var newAssetId = ImageAssetId.New();
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
            imageAssetId: oldAssetId,
            visibility: Visibility.Private);

        var repository = new SingleProductRepository(product);
        var cleanup = new RecordingCleanupService("storage_error");
        var handler = new UpdateProductCommandHandler(repository, cleanup, new StubUserRepository(User.Create("user@example.com", "hash")), FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        var command = new UpdateProductCommand(
            userId.Value,
            product.Id.Value,
            Barcode: null,
            ClearBarcode: false,
            Name: null,
            Brand: null,
            ClearBrand: false,
            ProductType: null,
            Category: null,
            ClearCategory: false,
            Description: null,
            ClearDescription: false,
            Comment: null,
            ClearComment: false,
            ImageUrl: null,
            ClearImageUrl: false,
            ImageAssetId: newAssetId.Value,
            ClearImageAssetId: false,
            BaseUnit: null,
            BaseAmount: null,
            DefaultPortionAmount: null,
            CaloriesPerBase: null,
            ProteinsPerBase: null,
            FatsPerBase: null,
            CarbsPerBase: null,
            FiberPerBase: null,
            AlcoholPerBase: null,
            Visibility: null);

        Result<ProductModel> result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(repository.GetByIdForUpdateCalled);
        Assert.True(repository.UpdateCalled);
        Assert.Equal(newAssetId, product.ImageAssetId);
        Assert.Equal([oldAssetId], cleanup.RequestedAssetIds);
    }

    [Fact]
    public async Task UpdateProductCommandHandler_WithAllUpdateSections_AppliesChangesAndCleansOldAsset() {
        var user = User.Create("update-product-all@example.com", "hash");
        var oldAssetId = ImageAssetId.New();
        var newAssetId = ImageAssetId.New();
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
            barcode: "111",
            brand: "Old brand",
            productType: ProductType.Fruit,
            category: "Fruit",
            description: "Old description",
            comment: "Old comment",
            imageUrl: "https://cdn.example/old.jpg",
            imageAssetId: oldAssetId,
            visibility: Visibility.Private);
        var repository = new SingleProductRepository(product);
        var cleanup = new RecordingCleanupService();
        var handler = new UpdateProductCommandHandler(
            repository,
            cleanup,
            new StubUserRepository(user),
            new FoodDiary.Application.Tests.RecordingImageAssetAccessService()
                .WithAsset(newAssetId, "https://cdn.example/new.jpg"));

        Result<ProductModel> result = await handler.Handle(
            CreateFullProductUpdateCommand(user.Id.Value, product.Id.Value, newAssetId.Value),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(repository.UpdateCalled);
        Assert.Equal("Updated apple", result.Value.Name);
        Assert.Equal("222", result.Value.Barcode);
        Assert.Null(result.Value.Brand);
        Assert.Null(result.Value.Category);
        Assert.Equal("Fresh description", result.Value.Description);
        Assert.Null(result.Value.Comment);
        Assert.Equal(newAssetId.Value, result.Value.ImageAssetId);
        Assert.Equal("https://cdn.example/new.jpg", result.Value.ImageUrl);
        Assert.Equal(nameof(MeasurementUnit.Pcs), result.Value.BaseUnit);
        Assert.Equal(2, result.Value.DefaultPortionAmount);
        Assert.Equal(80, result.Value.CaloriesPerBase);
        Assert.Equal(nameof(Visibility.Public), result.Value.Visibility);
        Assert.Equal([oldAssetId], cleanup.RequestedAssetIds);
    }

    [Fact]
    public async Task UpdateProductCommandHandler_WithEmptyImageAssetId_ReturnsValidationFailure() {
        var userId = UserId.New();
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
            visibility: Visibility.Private);

        var repository = new SingleProductRepository(product);
        var handler = new UpdateProductCommandHandler(
            repository,
            new RecordingCleanupService(),
            new StubUserRepository(User.Create("user@example.com", "hash")),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        Result<ProductModel> result = await handler.Handle(
            new UpdateProductCommand(
                userId.Value,
                product.Id.Value,
                Barcode: null,
                ClearBarcode: false,
                Name: null,
                Brand: null,
                ClearBrand: false,
                ProductType: null,
                Category: null,
                ClearCategory: false,
                Description: null,
                ClearDescription: false,
                Comment: null,
                ClearComment: false,
                ImageUrl: null,
                ClearImageUrl: false,
                ImageAssetId: Guid.Empty,
                ClearImageAssetId: false,
                BaseUnit: null,
                BaseAmount: null,
                DefaultPortionAmount: null,
                CaloriesPerBase: null,
                ProteinsPerBase: null,
                FatsPerBase: null,
                CarbsPerBase: null,
                FiberPerBase: null,
                AlcoholPerBase: null,
                Visibility: null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("ImageAssetId", result.Error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UpdateProductCommandHandler_WithEmptyProductId_ReturnsValidationFailure() {
        var handler = new UpdateProductCommandHandler(
            new NoopProductRepository(),
            new RecordingCleanupService(),
            new StubUserRepository(User.Create("user@example.com", "hash")),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        Result<ProductModel> result = await handler.Handle(
            new UpdateProductCommand(
                Guid.NewGuid(),
                Guid.Empty,
                Barcode: null,
                ClearBarcode: false,
                Name: null,
                Brand: null,
                ClearBrand: false,
                ProductType: null,
                Category: null,
                ClearCategory: false,
                Description: null,
                ClearDescription: false,
                Comment: null,
                ClearComment: false,
                ImageUrl: null,
                ClearImageUrl: false,
                ImageAssetId: null,
                ClearImageAssetId: false,
                BaseUnit: null,
                BaseAmount: null,
                DefaultPortionAmount: null,
                CaloriesPerBase: null,
                ProteinsPerBase: null,
                FatsPerBase: null,
                CarbsPerBase: null,
                FiberPerBase: null,
                AlcoholPerBase: null,
                Visibility: null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("ProductId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateProductCommandHandler_WithInvalidProductType_ReturnsValidationFailure() {
        var user = User.Create("update-product-invalid-type@example.com", "hash");
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
        var repository = new SingleProductRepository(product);
        var handler = new UpdateProductCommandHandler(
            repository,
            new RecordingCleanupService(),
            new StubUserRepository(user),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        Result<ProductModel> result = await handler.Handle(
            new UpdateProductCommand(
                user.Id.Value,
                product.Id.Value,
                Barcode: null,
                ClearBarcode: false,
                Name: null,
                Brand: null,
                ClearBrand: false,
                ProductType: "NotAProductType",
                Category: null,
                ClearCategory: false,
                Description: null,
                ClearDescription: false,
                Comment: null,
                ClearComment: false,
                ImageUrl: null,
                ClearImageUrl: false,
                ImageAssetId: null,
                ClearImageAssetId: false,
                BaseUnit: null,
                BaseAmount: null,
                DefaultPortionAmount: null,
                CaloriesPerBase: null,
                ProteinsPerBase: null,
                FatsPerBase: null,
                CarbsPerBase: null,
                FiberPerBase: null,
                AlcoholPerBase: null,
                Visibility: null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.False(repository.UpdateCalled);
    }

    [Fact]
    public async Task UpdateProductCommandHandler_WithMissingUserId_ReturnsInvalidToken() {
        var handler = new UpdateProductCommandHandler(
            new NoopProductRepository(),
            new RecordingCleanupService(),
            new StubUserRepository(User.Create("user@example.com", "hash")),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        Result<ProductModel> result = await handler.Handle(
            CreateUpdateProductCommand(userId: null, ProductId.New().Value),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task UpdateProductCommandHandler_WithInvalidBaseUnit_ReturnsValidationFailure() {
        var user = User.Create("update-product-invalid-unit@example.com", "hash");
        var handler = new UpdateProductCommandHandler(
            new NoopProductRepository(),
            new RecordingCleanupService(),
            new StubUserRepository(user),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        Result<ProductModel> result = await handler.Handle(
            CreateUpdateProductCommand(user.Id.Value, ProductId.New().Value, baseUnit: "Cup"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("BaseUnit", result.Error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UpdateProductCommandHandler_WithInvalidVisibility_ReturnsValidationFailure() {
        var user = User.Create("update-product-invalid-visibility@example.com", "hash");
        var handler = new UpdateProductCommandHandler(
            new NoopProductRepository(),
            new RecordingCleanupService(),
            new StubUserRepository(user),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        Result<ProductModel> result = await handler.Handle(
            CreateUpdateProductCommand(user.Id.Value, ProductId.New().Value, visibility: "Hidden"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("Visibility", result.Error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UpdateProductCommandHandler_WhenProductIsMissing_ReturnsNotAccessible() {
        var user = User.Create("update-product-missing@example.com", "hash");
        var productId = ProductId.New();
        var handler = new UpdateProductCommandHandler(
            new NoopProductRepository(),
            new RecordingCleanupService(),
            new StubUserRepository(user),
            FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        Result<ProductModel> result = await handler.Handle(
            CreateUpdateProductCommand(user.Id.Value, productId.Value, name: "Updated"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Product.NotAccessible", result.Error.Code);
    }

    [Fact]
    public async Task UpdateProductCommandHandler_WhenImageAssetAccessFails_ReturnsFailure() {
        var user = User.Create("update-product-forbidden-image@example.com", "hash");
        RecordingImageAssetAccessService access = new FoodDiary.Application.Tests.RecordingImageAssetAccessService()
            .WithFailure(Errors.Image.Forbidden());
        var handler = new UpdateProductCommandHandler(
            new NoopProductRepository(),
            new RecordingCleanupService(),
            new StubUserRepository(user),
            access);

        Result<ProductModel> result = await handler.Handle(
            CreateUpdateProductCommand(user.Id.Value, ProductId.New().Value, imageAssetId: Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Image.Forbidden", result.Error.Code);
    }

    [Fact]
    public async Task GetProductsQueryHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-product@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new GetProductsQueryHandler(new NoopProductRepository(), new StubUserRepository(user));

        Result<PagedResponse<ProductModel>> result = await handler.Handle(
            new GetProductsQuery(user.Id.Value, 1, 10, Search: null, IncludePublic: true),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task CreateProductCommandHandler_WithValidCommand_PersistsAndReturnsOwnedModel() {
        var user = User.Create("create-product@example.com", "hash");
        var repository = new NoopProductRepository();
        var handler = new CreateProductCommandHandler(repository, new StubUserRepository(user), FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        Result<ProductModel> result = await handler.Handle(
            new CreateProductCommand(
                user.Id.Value,
                "4601234567890",
                "Apple",
                "Farm",
                "Other",
                "Fruit",
                "Fresh apple",
                "Owner note",
                "https://cdn.test/apple.png",
                ImageAssetId: null,
                "G",
                100,
                120,
                52,
                0.3,
                0.2,
                14,
                2.4,
                0,
                "Private"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(repository.LastAddedProduct);
        Assert.Equal("Apple", repository.LastAddedProduct.Name);
        Assert.Equal("Farm", repository.LastAddedProduct.Brand);
        Assert.True(result.Value.IsOwnedByCurrentUser);
        Assert.Equal("Owner note", result.Value.Comment);
    }

    [Fact]
    public async Task CreateProductCommandHandler_WithImageAsset_UsesResolvedAssetUrl() {
        var user = User.Create("create-product-image@example.com", "hash");
        var assetId = ImageAssetId.New();
        RecordingImageAssetAccessService access = new FoodDiary.Application.Tests.RecordingImageAssetAccessService()
            .WithAsset(assetId, "https://cdn.test/assets/product.webp");
        var repository = new NoopProductRepository();
        var handler = new CreateProductCommandHandler(repository, new StubUserRepository(user), access);

        Result<ProductModel> result = await handler.Handle(
            new CreateProductCommand(
                user.Id.Value,
                Barcode: null,
                Name: "Apple",
                Brand: null,
                ProductType: "Other",
                Category: null,
                Description: null,
                Comment: null,
                ImageUrl: "https://client.test/not-trusted.webp",
                ImageAssetId: assetId.Value,
                BaseUnit: "G",
                BaseAmount: 100,
                DefaultPortionAmount: 100,
                CaloriesPerBase: 52,
                ProteinsPerBase: 0.3,
                FatsPerBase: 0.2,
                CarbsPerBase: 14,
                FiberPerBase: 2.4,
                AlcoholPerBase: 0,
                Visibility: "Private"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal([assetId], access.RequestedAssetIds);
        Assert.Equal("https://cdn.test/assets/product.webp", repository.LastAddedProduct!.ImageUrl);
        Assert.Equal(assetId, repository.LastAddedProduct.ImageAssetId);
    }

    [Fact]
    public async Task CreateProductCommandHandler_WhenImageAssetAccessFails_DoesNotPersist() {
        var user = User.Create("create-product-forbidden-image@example.com", "hash");
        var repository = new NoopProductRepository();
        RecordingImageAssetAccessService access = new FoodDiary.Application.Tests.RecordingImageAssetAccessService()
            .WithFailure(Errors.Image.Forbidden());
        var handler = new CreateProductCommandHandler(repository, new StubUserRepository(user), access);

        Result<ProductModel> result = await handler.Handle(
            new CreateProductCommand(
                user.Id.Value,
                Barcode: null,
                Name: "Apple",
                Brand: null,
                ProductType: "Other",
                Category: null,
                Description: null,
                Comment: null,
                ImageUrl: null,
                ImageAssetId: Guid.NewGuid(),
                BaseUnit: "G",
                BaseAmount: 100,
                DefaultPortionAmount: 100,
                CaloriesPerBase: 52,
                ProteinsPerBase: 0.3,
                FatsPerBase: 0.2,
                CarbsPerBase: 14,
                FiberPerBase: 2.4,
                AlcoholPerBase: 0,
                Visibility: "Private"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Image.Forbidden", result.Error.Code);
        Assert.Null(repository.LastAddedProduct);
    }

    [Fact]
    public async Task GetProductByIdQueryHandler_WithAccessibleProduct_ReturnsUsageAndOwnerComment() {
        var user = User.Create("product-by-id@example.com", "hash");
        var product = Product.Create(
            user.Id,
            name: "Chicken",
            baseUnit: MeasurementUnit.G,
            baseAmount: 100,
            defaultPortionAmount: 100,
            caloriesPerBase: 165,
            proteinsPerBase: 31,
            fatsPerBase: 3.6,
            carbsPerBase: 0,
            fiberPerBase: 0,
            alcoholPerBase: 0,
            comment: "Private note",
            visibility: Visibility.Private);

        SetProductUsageCollections(product, mealItemsCount: 2, recipeIngredientsCount: 1);
        var handler = new GetProductByIdQueryHandler(new SingleProductRepository(product), new StubUserRepository(user));

        Result<ProductModel> result = await handler.Handle(new GetProductByIdQuery(user.Id.Value, product.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(product.Id.Value, result.Value.Id);
        Assert.Equal(3, result.Value.UsageCount);
        Assert.True(result.Value.IsOwnedByCurrentUser);
        Assert.Equal("Private note", result.Value.Comment);
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
        var handler = new DuplicateProductCommandHandler(repository);

        Result<ProductModel> result = await handler.Handle(new DuplicateProductCommand(user.Id.Value, original.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(repository.LastAddedProduct);
        Assert.NotEqual(original.Id, repository.LastAddedProduct.Id);
        Assert.Equal(original.Name, repository.LastAddedProduct.Name);
        Assert.Equal(original.Barcode, repository.LastAddedProduct.Barcode);
        Assert.Equal(original.ImageUrl, repository.LastAddedProduct.ImageUrl);
        Assert.Null(repository.LastAddedProduct.ImageAssetId);
        Assert.Equal(user.Id, repository.LastAddedProduct.UserId);
        Assert.True(result.Value.IsOwnedByCurrentUser);
    }

    [Fact]
    public async Task UpdateProductCommandHandler_WithoutChanges_DoesNotPersistOrCleanup() {
        var user = User.Create("update-product@example.com", "hash");
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

        var repository = new SingleProductRepository(product);
        var cleanup = new RecordingCleanupService();
        var handler = new UpdateProductCommandHandler(repository, cleanup, new StubUserRepository(user), FoodDiary.Application.Tests.AllowImageAssetAccessService.Instance);

        Result<ProductModel> result = await handler.Handle(
            new UpdateProductCommand(
                user.Id.Value,
                product.Id.Value,
                Barcode: null,
                ClearBarcode: false,
                Name: null,
                Brand: null,
                ClearBrand: false,
                ProductType: null,
                Category: null,
                ClearCategory: false,
                Description: null,
                ClearDescription: false,
                Comment: null,
                ClearComment: false,
                ImageUrl: null,
                ClearImageUrl: false,
                ImageAssetId: null,
                ClearImageAssetId: false,
                BaseUnit: null,
                BaseAmount: null,
                DefaultPortionAmount: null,
                CaloriesPerBase: null,
                ProteinsPerBase: null,
                FatsPerBase: null,
                CarbsPerBase: null,
                FiberPerBase: null,
                AlcoholPerBase: null,
                Visibility: null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(repository.GetByIdForUpdateCalled);
        Assert.False(repository.UpdateCalled);
        Assert.Empty(cleanup.RequestedAssetIds);
    }

    [Fact]
    public async Task GetProductsOverviewQueryHandler_WithoutSearch_ReturnsRecentFavoritesAndFavoriteFlags() {
        var user = User.Create("overview-products@example.com", "hash");
        var breakfast = Product.Create(
            user.Id,
            name: "Breakfast Yogurt",
            baseUnit: MeasurementUnit.G,
            baseAmount: 100,
            defaultPortionAmount: 150,
            caloriesPerBase: 73,
            proteinsPerBase: 9.5,
            fatsPerBase: 2.1,
            carbsPerBase: 3.8,
            fiberPerBase: 0,
            alcoholPerBase: 0,
            visibility: Visibility.Private);
        var lunch = Product.Create(
            user.Id,
            name: "Lunch Chicken",
            baseUnit: MeasurementUnit.G,
            baseAmount: 100,
            defaultPortionAmount: 120,
            caloriesPerBase: 165,
            proteinsPerBase: 31,
            fatsPerBase: 3.6,
            carbsPerBase: 0,
            fiberPerBase: 0,
            alcoholPerBase: 0,
            visibility: Visibility.Private);

        var favorite = FavoriteProduct.Create(user.Id, lunch.Id, "Fav lunch");
        SetFavoriteProductNavigation(favorite, lunch);

        var repository = new OverviewProductRepository(
            pagedItems: [(breakfast, 2), (lunch, 5)],
            productsByIdWithUsage: new Dictionary<ProductId, (Product Product, int UsageCount)> {
                [lunch.Id] = (lunch, 5),
            });
        var recentRepository = new StubRecentItemRepository([
            new RecentProductUsage(lunch.Id, 5, DateTime.UtcNow),
        ]);
        var favoriteRepository = new StubFavoriteProductRepository([favorite]);
        var handler = new GetProductsOverviewQueryHandler(repository, recentRepository, favoriteRepository);

        Result<ProductOverviewModel> result = await handler.Handle(
            new GetProductsOverviewQuery(user.Id.Value, 1, 10, Search: null, IncludePublic: true, 10, 10),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.AllProducts.Data.Count);
        Assert.Single(result.Value.RecentItems);
        Assert.Single(result.Value.FavoriteItems);
        Assert.Equal(1, result.Value.FavoriteTotalCount);
        Assert.Equal(lunch.Id.Value, result.Value.RecentItems[0].Id);
        Assert.True(result.Value.RecentItems[0].IsFavorite);
        Assert.Equal(favorite.Id.Value, result.Value.RecentItems[0].FavoriteProductId);
        Assert.True(result.Value.AllProducts.Data.Single(x => x.Id == lunch.Id.Value).IsFavorite);
    }

    [Fact]
    public async Task GetProductsOverviewQueryHandler_WithSearch_SkipsRecentItems() {
        var user = User.Create("overview-search@example.com", "hash");
        var product = Product.Create(
            user.Id,
            name: "Protein Bar",
            baseUnit: MeasurementUnit.G,
            baseAmount: 100,
            defaultPortionAmount: 60,
            caloriesPerBase: 380,
            proteinsPerBase: 30,
            fatsPerBase: 10,
            carbsPerBase: 40,
            fiberPerBase: 5,
            alcoholPerBase: 0,
            visibility: Visibility.Private);

        var repository = new OverviewProductRepository(pagedItems: [(product, 1)]);
        var recentRepository = new StubRecentItemRepository([
            new RecentProductUsage(product.Id, 1, DateTime.UtcNow),
        ]);
        var favoriteRepository = new StubFavoriteProductRepository([]);
        var handler = new GetProductsOverviewQueryHandler(repository, recentRepository, favoriteRepository);

        Result<ProductOverviewModel> result = await handler.Handle(
            new GetProductsOverviewQuery(user.Id.Value, 1, 10, "protein", IncludePublic: true, 10, 10),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.RecentItems);
        Assert.Equal(0, recentRepository.GetRecentProductsCallCount);
    }

    [Fact]
    public async Task GetProductsOverviewQueryHandler_WithMissingUserId_ReturnsInvalidToken() {
        var handler = new GetProductsOverviewQueryHandler(
            new OverviewProductRepository(),
            new StubRecentItemRepository([]),
            new StubFavoriteProductRepository([]));

        Result<ProductOverviewModel> result = await handler.Handle(
            new GetProductsOverviewQuery(UserId: null, 1, 10, Search: null, IncludePublic: true),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetProductsOverviewQueryHandler_WhenNoRecentProducts_ReturnsEmptyRecentItems() {
        var user = User.Create("overview-empty-recents@example.com", "hash");
        var product = Product.Create(
            user.Id,
            name: "Apple",
            baseUnit: MeasurementUnit.G,
            baseAmount: 100,
            defaultPortionAmount: 120,
            caloriesPerBase: 52,
            proteinsPerBase: 0.3,
            fatsPerBase: 0.2,
            carbsPerBase: 14,
            fiberPerBase: 2.4,
            alcoholPerBase: 0,
            visibility: Visibility.Private);
        var recentRepository = new StubRecentItemRepository([]);
        var handler = new GetProductsOverviewQueryHandler(
            new OverviewProductRepository(pagedItems: [(product, 1)]),
            recentRepository,
            new StubFavoriteProductRepository([]));

        Result<ProductOverviewModel> result = await handler.Handle(
            new GetProductsOverviewQuery(user.Id.Value, 1, 10, Search: null, IncludePublic: true, 10, 10),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.RecentItems);
        Assert.Equal(1, recentRepository.GetRecentProductsCallCount);
    }

    [Fact]
    public async Task GetProductsOverviewQueryHandler_WithProductTypes_FiltersDistinctValidTypes() {
        var user = User.Create("overview-product-types@example.com", "hash");
        var dairy = Product.Create(
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
            productType: ProductType.Dairy,
            visibility: Visibility.Private);
        var fruit = Product.Create(
            user.Id,
            name: "Apple",
            baseUnit: MeasurementUnit.G,
            baseAmount: 100,
            defaultPortionAmount: 120,
            caloriesPerBase: 52,
            proteinsPerBase: 0.3,
            fatsPerBase: 0.2,
            carbsPerBase: 14,
            fiberPerBase: 2.4,
            alcoholPerBase: 0,
            productType: ProductType.Fruit,
            visibility: Visibility.Private);
        var handler = new GetProductsOverviewQueryHandler(
            new OverviewProductRepository(pagedItems: [(dairy, 3), (fruit, 2)]),
            new StubRecentItemRepository([]),
            new StubFavoriteProductRepository([]));

        Result<ProductOverviewModel> result = await handler.Handle(
            new GetProductsOverviewQuery(
                user.Id.Value,
                Page: 1,
                Limit: 10,
                Search: null,
                IncludePublic: true,
                ProductTypes: ["dairy", "Dairy", "not-a-type"]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        ProductModel item = Assert.Single(result.Value.AllProducts.Data);
        Assert.Equal(dairy.Id.Value, item.Id);
        Assert.Equal(ProductType.Dairy.ToString(), item.ProductType);
    }

    [Fact]
    public async Task GetRecentProductsQueryHandler_WithMissingUserId_ReturnsInvalidToken() {
        var handler = new GetRecentProductsQueryHandler(new StubRecentItemRepository([]), new NoopProductRepository());

        Result<IReadOnlyList<ProductModel>> result = await handler.Handle(new GetRecentProductsQuery(UserId: null, 10, IncludePublic: true), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetRecentProductsQueryHandler_WhenNoRecentProducts_ReturnsEmptyList() {
        var userId = UserId.New();
        var recentRepository = new StubRecentItemRepository([]);
        var handler = new GetRecentProductsQueryHandler(recentRepository, new NoopProductRepository());

        Result<IReadOnlyList<ProductModel>> result = await handler.Handle(new GetRecentProductsQuery(userId.Value, 10, IncludePublic: true), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
        Assert.Equal(1, recentRepository.GetRecentProductsCallCount);
    }

    [Fact]
    public async Task GetRecentProductsQueryHandler_ReturnsProductsInRecentOrderAndSkipsMissingItems() {
        var userId = UserId.New();
        var owned = Product.Create(
            userId,
            name: "Owned Apple",
            baseUnit: MeasurementUnit.G,
            baseAmount: 100,
            defaultPortionAmount: 120,
            caloriesPerBase: 52,
            proteinsPerBase: 0.3,
            fatsPerBase: 0.2,
            carbsPerBase: 14,
            fiberPerBase: 2.4,
            alcoholPerBase: 0,
            visibility: Visibility.Private);
        var publicProduct = Product.Create(
            UserId.New(),
            name: "Public Yogurt",
            baseUnit: MeasurementUnit.G,
            baseAmount: 100,
            defaultPortionAmount: 150,
            caloriesPerBase: 73,
            proteinsPerBase: 9.5,
            fatsPerBase: 2.1,
            carbsPerBase: 3.8,
            fiberPerBase: 0,
            alcoholPerBase: 0,
            visibility: Visibility.Public);
        var missingProductId = ProductId.New();
        var repository = new OverviewProductRepository(
            productsByIdWithUsage: new Dictionary<ProductId, (Product Product, int UsageCount)> {
                [owned.Id] = (owned, 7),
                [publicProduct.Id] = (publicProduct, 3),
            });
        var recentRepository = new StubRecentItemRepository([
            new RecentProductUsage(publicProduct.Id, 3, DateTime.UtcNow),
            new RecentProductUsage(missingProductId, 9, DateTime.UtcNow),
            new RecentProductUsage(owned.Id, 7, DateTime.UtcNow),
        ]);
        var handler = new GetRecentProductsQueryHandler(recentRepository, repository);

        Result<IReadOnlyList<ProductModel>> result = await handler.Handle(new GetRecentProductsQuery(userId.Value, 99, IncludePublic: true), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.Equal([publicProduct.Id.Value, owned.Id.Value], [.. result.Value.Select(x => x.Id)]);
        Assert.False(result.Value[0].IsOwnedByCurrentUser);
        Assert.True(result.Value[1].IsOwnedByCurrentUser);
        Assert.Equal(3, result.Value[0].UsageCount);
        Assert.Equal(7, result.Value[1].UsageCount);
    }

    [ExcludeFromCodeCoverage]
    private sealed class NoopProductRepository : IProductRepository {
        public Product? LastAddedProduct { get; private set; }

        public Task<Product> AddAsync(Product product, CancellationToken cancellationToken = default) {
            LastAddedProduct = product;
            return Task.FromResult(product);
        }

        public Task<(IReadOnlyList<(Product Product, int UsageCount)> Items, int TotalItems)> GetPagedAsync(
            UserId userId,
            bool includePublic,
            int page,
            int limit,
            string? search,
            IReadOnlyCollection<ProductType>? productTypes = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult((Items: (IReadOnlyList<(Product Product, int UsageCount)>)[], TotalItems: 0));

        public Task<Product?> GetByIdAsync(
            ProductId id,
            UserId userId,
            bool includePublic = true,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Product?>(null);

        public Task<IReadOnlyDictionary<ProductId, Product>> GetByIdsAsync(
            IEnumerable<ProductId> ids,
            UserId userId,
            bool includePublic = true,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<ProductId, Product>>(new Dictionary<ProductId, Product>());

        public Task<IReadOnlyDictionary<ProductId, (Product Product, int UsageCount)>> GetByIdsWithUsageAsync(
            IEnumerable<ProductId> ids,
            UserId userId,
            bool includePublic = true,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<ProductId, (Product Product, int UsageCount)>>(new Dictionary<ProductId, (Product Product, int UsageCount)>());

        public Task UpdateAsync(Product product, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(Product product, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    [ExcludeFromCodeCoverage]
    private sealed class SingleProductRepository(Product product) : IProductRepository {
        public bool DeleteCalled { get; private set; }
        public bool UpdateCalled { get; private set; }
        public bool GetByIdForUpdateCalled { get; private set; }
        public Product? LastAddedProduct { get; private set; }

        public Task<Product> AddAsync(Product product, CancellationToken cancellationToken = default) {
            LastAddedProduct = product;
            return Task.FromResult(product);
        }

        public Task<(IReadOnlyList<(Product Product, int UsageCount)> Items, int TotalItems)> GetPagedAsync(
            UserId userId,
            bool includePublic,
            int page,
            int limit,
            string? search,
            IReadOnlyCollection<ProductType>? productTypes = null,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<Product?> GetByIdAsync(
            ProductId id,
            UserId userId,
            bool includePublic = true,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Product?>(id == product.Id && userId == product.UserId ? product : null);

        public Task<Product?> GetByIdForUpdateAsync(
            ProductId id,
            UserId userId,
            bool includePublic = true,
            CancellationToken cancellationToken = default) {
            GetByIdForUpdateCalled = true;
            return GetByIdAsync(id, userId, includePublic, cancellationToken);
        }

        public Task<IReadOnlyDictionary<ProductId, Product>> GetByIdsAsync(
            IEnumerable<ProductId> ids,
            UserId userId,
            bool includePublic = true,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IReadOnlyDictionary<ProductId, (Product Product, int UsageCount)>> GetByIdsWithUsageAsync(
            IEnumerable<ProductId> ids,
            UserId userId,
            bool includePublic = true,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task UpdateAsync(Product product, CancellationToken cancellationToken = default) {
            UpdateCalled = true;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Product product, CancellationToken cancellationToken = default) {
            DeleteCalled = true;
            return Task.CompletedTask;
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingCleanupService(string? errorCode = null) : IImageAssetCleanupService {
        public List<ImageAssetId> RequestedAssetIds { get; } = [];

        public Task<DeleteImageAssetResult> DeleteIfUnusedAsync(ImageAssetId assetId, CancellationToken cancellationToken = default) {
            RequestedAssetIds.Add(assetId);
            return Task.FromResult(errorCode is null
                ? new DeleteImageAssetResult(Deleted: true)
                : new DeleteImageAssetResult(Deleted: false, errorCode));
        }

        public Task<int> CleanupOrphansAsync(DateTime olderThanUtc, int batchSize, CancellationToken cancellationToken = default) =>
            Task.FromResult(0);
    }

    [ExcludeFromCodeCoverage]
    private sealed class OverviewProductRepository(
        IReadOnlyList<(Product Product, int UsageCount)>? pagedItems = null,
        IReadOnlyDictionary<ProductId, (Product Product, int UsageCount)>? productsByIdWithUsage = null) : IProductRepository {
        private readonly IReadOnlyList<(Product Product, int UsageCount)> _pagedItems = pagedItems ?? [];
        private readonly IReadOnlyDictionary<ProductId, (Product Product, int UsageCount)> _productsByIdWithUsage = productsByIdWithUsage ?? new Dictionary<ProductId, (Product Product, int UsageCount)>();

        public Task<Product> AddAsync(Product product, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<(IReadOnlyList<(Product Product, int UsageCount)> Items, int TotalItems)> GetPagedAsync(
            UserId userId,
            bool includePublic,
            int page,
            int limit,
            string? search,
            IReadOnlyCollection<ProductType>? productTypes = null,
            CancellationToken cancellationToken = default) {
            var filtered = _pagedItems
                .Where(item => productTypes?.Contains(item.Product.ProductType) != false)
                .ToList();
            return Task.FromResult(((IReadOnlyList<(Product Product, int UsageCount)>)filtered, filtered.Count));
        }

        public Task<Product?> GetByIdAsync(
            ProductId id,
            UserId userId,
            bool includePublic = true,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Product?>(_pagedItems.Select(x => x.Product).FirstOrDefault(x => x.Id == id && x.UserId == userId));

        public Task<IReadOnlyDictionary<ProductId, Product>> GetByIdsAsync(
            IEnumerable<ProductId> ids,
            UserId userId,
            bool includePublic = true,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<ProductId, Product>>(new Dictionary<ProductId, Product>());

        public Task<IReadOnlyDictionary<ProductId, (Product Product, int UsageCount)>> GetByIdsWithUsageAsync(
            IEnumerable<ProductId> ids,
            UserId userId,
            bool includePublic = true,
            CancellationToken cancellationToken = default) {
            var idSet = ids.ToHashSet();
            var filtered = _productsByIdWithUsage
                .Where(pair => idSet.Contains(pair.Key))
                .ToDictionary();
            return Task.FromResult<IReadOnlyDictionary<ProductId, (Product Product, int UsageCount)>>(filtered);
        }

        public Task UpdateAsync(Product product, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAsync(Product product, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubRecentItemRepository(IReadOnlyList<RecentProductUsage> recentProducts) : IRecentItemRepository {
        private readonly IReadOnlyList<RecentProductUsage> _recentProducts = recentProducts;
        public int GetRecentProductsCallCount { get; private set; }

        public Task RegisterUsageAsync(
            UserId userId,
            IReadOnlyCollection<ProductId> productIds,
            IReadOnlyCollection<RecipeId> recipeIds,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<RecentProductUsage>> GetRecentProductsAsync(
            UserId userId,
            int limit,
            CancellationToken cancellationToken = default) {
            GetRecentProductsCallCount++;
            return Task.FromResult<IReadOnlyList<RecentProductUsage>>(_recentProducts.Take(limit).ToList());
        }

        public Task<IReadOnlyList<RecentRecipeUsage>> GetRecentRecipesAsync(
            UserId userId,
            int limit,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubFavoriteProductRepository(IReadOnlyList<FavoriteProduct> favorites) : IFavoriteProductRepository {
        private readonly IReadOnlyList<FavoriteProduct> _favorites = favorites;

        public Task<FavoriteProduct> AddAsync(FavoriteProduct favorite, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAsync(FavoriteProduct favorite, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAsync(FavoriteProduct favorite, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<FavoriteProduct?> GetByIdAsync(FavoriteProductId id, UserId userId, bool asTracking = false, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<FavoriteProduct?> GetByProductIdAsync(ProductId productId, UserId userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyDictionary<ProductId, FavoriteProduct>> GetByProductIdsAsync(UserId userId, IReadOnlyCollection<ProductId> productIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<ProductId, FavoriteProduct>>(_favorites.Where(f => productIds.Contains(f.ProductId)).ToDictionary(f => f.ProductId));
        public Task<IReadOnlyList<FavoriteProduct>> GetAllAsync(UserId userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_favorites);
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubUserRepository(User user) : IUserRepository {
        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByEmailIncludingDeletedAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default) => Task.FromResult<User?>(user);
        public Task<User?> GetByIdIncludingDeletedAsync(UserId id, CancellationToken cancellationToken = default) => Task.FromResult<User?>(user);
        public Task<User?> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByTelegramUserIdIncludingDeletedAsync(long telegramUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<User> Items, int TotalItems)> GetPagedAsync(string? search, int page, int limit, bool includeDeleted, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<User> RecentUsers)> GetAdminDashboardSummaryAsync(int recentLimit, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Role>> GetRolesByNamesAsync(IReadOnlyList<string> names, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User> AddAsync(User addedUser, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAsync(User updatedUser, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private static void SetFavoriteProductNavigation(FavoriteProduct favorite, Product product) {
        typeof(FavoriteProduct)
            .GetProperty(nameof(FavoriteProduct.Product))!
            .SetValue(favorite, product);
    }

    private static void SetProductUsageCollections(Product product, int mealItemsCount, int recipeIngredientsCount) {
        var mealItems = Enumerable.Range(0, mealItemsCount)
            .Select(_ => (FoodDiary.Domain.Entities.Meals.MealItem)null!)
            .ToList();
        var recipeIngredients = Enumerable.Range(0, recipeIngredientsCount)
            .Select(_ => (FoodDiary.Domain.Entities.Recipes.RecipeIngredient)null!)
            .ToList();

        typeof(Product)
            .GetField("_mealItems", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .SetValue(product, mealItems);
        typeof(Product)
            .GetField("_recipeIngredients", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .SetValue(product, recipeIngredients);
    }

    private static CreateProductCommand CreateProductCommand(
        Guid? userId,
        string productType = "Other",
        string baseUnit = "G",
        string visibility = "Private") =>
        new(
            userId,
            Barcode: null,
            Name: "Apple",
            Brand: null,
            ProductType: productType,
            Category: null,
            Description: null,
            Comment: null,
            ImageUrl: null,
            ImageAssetId: null,
            BaseUnit: baseUnit,
            BaseAmount: 100,
            DefaultPortionAmount: 100,
            CaloriesPerBase: 52,
            ProteinsPerBase: 0.3,
            FatsPerBase: 0.2,
            CarbsPerBase: 14,
            FiberPerBase: 2.4,
            AlcoholPerBase: 0,
            Visibility: visibility);

    private static UpdateProductCommand CreateUpdateProductCommand(
        Guid? userId,
        Guid productId,
        string? name = null,
        string? baseUnit = null,
        Guid? imageAssetId = null,
        string? visibility = null) =>
        new(
            userId,
            productId,
            Barcode: null,
            ClearBarcode: false,
            Name: name,
            Brand: null,
            ClearBrand: false,
            ProductType: null,
            Category: null,
            ClearCategory: false,
            Description: null,
            ClearDescription: false,
            Comment: null,
            ClearComment: false,
            ImageUrl: null,
            ClearImageUrl: false,
            ImageAssetId: imageAssetId,
            ClearImageAssetId: false,
            BaseUnit: baseUnit,
            BaseAmount: null,
            DefaultPortionAmount: null,
            CaloriesPerBase: null,
            ProteinsPerBase: null,
            FatsPerBase: null,
            CarbsPerBase: null,
            FiberPerBase: null,
            AlcoholPerBase: null,
            Visibility: visibility);

    private static UpdateProductCommand CreateFullProductUpdateCommand(
        Guid userId,
        Guid productId,
        Guid imageAssetId) =>
        new(
            userId,
            productId,
            Barcode: "222",
            ClearBarcode: false,
            Name: "Updated apple",
            Brand: null,
            ClearBrand: true,
            ProductType: nameof(ProductType.Dairy),
            Category: null,
            ClearCategory: true,
            Description: "Fresh description",
            ClearDescription: false,
            Comment: null,
            ClearComment: true,
            ImageUrl: "https://ignored.example/manual.jpg",
            ClearImageUrl: false,
            ImageAssetId: imageAssetId,
            ClearImageAssetId: false,
            BaseUnit: nameof(MeasurementUnit.Pcs),
            BaseAmount: 1,
            DefaultPortionAmount: 2,
            CaloriesPerBase: 80,
            ProteinsPerBase: 1.1,
            FatsPerBase: 0.5,
            CarbsPerBase: 20,
            FiberPerBase: 3,
            AlcoholPerBase: 0,
            Visibility: nameof(Visibility.Public));
}
