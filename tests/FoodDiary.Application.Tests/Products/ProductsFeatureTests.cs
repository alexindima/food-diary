using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Images.Common;
using FoodDiary.Application.Products.Commands.DeleteProduct;
using FoodDiary.Application.Products.Commands.CreateProduct;
using FoodDiary.Application.Products.Commands.DuplicateProduct;
using FoodDiary.Application.Products.Commands.UpdateProduct;
using FoodDiary.Application.Products.Queries.GetProductById;
using FoodDiary.Application.Products.Queries.GetProducts;
using FoodDiary.Application.Products.Queries.GetProductsWithRecent;
using FoodDiary.Application.Products.Queries.GetRecentProducts;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Products;

public class ProductsFeatureTests {
    [Fact]
    public async Task GetProductsWithRecentQueryValidator_WithEmptyUserId_Fails() {
        var validator = new GetProductsWithRecentQueryValidator();
        var query = new GetProductsWithRecentQuery(Guid.Empty, 1, 10, null, true);

        var result = await validator.ValidateAsync(query);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task GetRecentProductsQueryValidator_WithValidUserId_Passes() {
        var validator = new GetRecentProductsQueryValidator();
        var query = new GetRecentProductsQuery(Guid.NewGuid(), 10, true);

        var result = await validator.ValidateAsync(query);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task GetProductsQueryHandler_WithMissingUserId_ReturnsInvalidToken() {
        var handler = new GetProductsQueryHandler(new NoopProductRepository(), new StubUserRepository(User.Create("user@example.com", "hash")));
        var query = new GetProductsQuery(null, 1, 10, null, true);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task CreateProductCommandHandler_WithMissingUserId_ReturnsInvalidToken() {
        var handler = new CreateProductCommandHandler(new NoopProductRepository(), new StubUserRepository(User.Create("user@example.com", "hash")));
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

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task CreateProductCommandHandler_WithEmptyImageAssetId_ReturnsValidationFailure() {
        var handler = new CreateProductCommandHandler(new NoopProductRepository(), new StubUserRepository(User.Create("user@example.com", "hash")));
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

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("ImageAssetId", result.Error.Message, StringComparison.Ordinal);
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

        var result = await handler.Handle(new DeleteProductCommand(userId.Value, product.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(repository.DeleteCalled);
        Assert.Equal([assetId], cleanup.RequestedAssetIds);
    }

    [Fact]
    public async Task DeleteProductCommandHandler_WithEmptyProductId_ReturnsValidationFailure() {
        var handler = new DeleteProductCommandHandler(
            new NoopProductRepository(),
            new RecordingCleanupService(),
            new StubUserRepository(User.Create("user@example.com", "hash")));

        var result = await handler.Handle(
            new DeleteProductCommand(Guid.NewGuid(), Guid.Empty),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("ProductId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetProductByIdQueryHandler_WithEmptyProductId_ReturnsValidationFailure() {
        var handler = new GetProductByIdQueryHandler(new NoopProductRepository(), new StubUserRepository(User.Create("user@example.com", "hash")));

        var result = await handler.Handle(
            new GetProductByIdQuery(Guid.NewGuid(), Guid.Empty),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("ProductId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DuplicateProductCommandHandler_WithEmptyProductId_ReturnsValidationFailure() {
        var handler = new DuplicateProductCommandHandler(new NoopProductRepository());

        var result = await handler.Handle(
            new DuplicateProductCommand(Guid.NewGuid(), Guid.Empty),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("ProductId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
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
        var handler = new UpdateProductCommandHandler(repository, cleanup, new StubUserRepository(User.Create("user@example.com", "hash")));

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

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(repository.UpdateCalled);
        Assert.Equal(newAssetId, product.ImageAssetId);
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
            new StubUserRepository(User.Create("user@example.com", "hash")));

        var result = await handler.Handle(
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
            new StubUserRepository(User.Create("user@example.com", "hash")));

        var result = await handler.Handle(
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
    public async Task GetProductsQueryHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-product@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new GetProductsQueryHandler(new NoopProductRepository(), new StubUserRepository(user));

        var result = await handler.Handle(
            new GetProductsQuery(user.Id.Value, 1, 10, null, true),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    private sealed class NoopProductRepository : IProductRepository {
        public Task<Product> AddAsync(Product product, CancellationToken cancellationToken = default) => Task.FromResult(product);

        public Task<(IReadOnlyList<(Product Product, int UsageCount)> Items, int TotalItems)> GetPagedAsync(
            UserId userId,
            bool includePublic,
            int page,
            int limit,
            string? search,
            IReadOnlyCollection<ProductType>? productTypes = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult((Items: (IReadOnlyList<(Product Product, int UsageCount)>)Array.Empty<(Product Product, int UsageCount)>(), TotalItems: 0));

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

    private sealed class SingleProductRepository(Product product) : IProductRepository {
        public bool DeleteCalled { get; private set; }
        public bool UpdateCalled { get; private set; }

        public Task<Product> AddAsync(Product product, CancellationToken cancellationToken = default) => throw new NotSupportedException();

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

    private sealed class RecordingCleanupService(string? errorCode = null) : IImageAssetCleanupService {
        public List<ImageAssetId> RequestedAssetIds { get; } = [];

        public Task<DeleteImageAssetResult> DeleteIfUnusedAsync(ImageAssetId assetId, CancellationToken cancellationToken = default) {
            RequestedAssetIds.Add(assetId);
            return Task.FromResult(errorCode is null
                ? new DeleteImageAssetResult(true)
                : new DeleteImageAssetResult(false, errorCode));
        }

        public Task<int> CleanupOrphansAsync(DateTime olderThanUtc, int batchSize, CancellationToken cancellationToken = default) =>
            Task.FromResult(0);
    }

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
}
