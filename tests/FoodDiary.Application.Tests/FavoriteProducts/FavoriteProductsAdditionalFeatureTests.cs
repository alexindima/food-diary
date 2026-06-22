using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.FavoriteProducts.Common;
using FoodDiary.Application.FavoriteProducts.Commands.AddFavoriteProduct;
using FoodDiary.Application.FavoriteProducts.Commands.RemoveFavoriteProduct;
using FoodDiary.Application.FavoriteProducts.Commands.UpdateFavoriteProduct;
using FoodDiary.Application.FavoriteProducts.Models;
using FoodDiary.Application.FavoriteProducts.Queries.GetFavoriteProducts;
using FoodDiary.Application.FavoriteProducts.Queries.IsProductFavorite;
using FoodDiary.Domain.Entities.FavoriteProducts;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.FavoriteProducts;

[ExcludeFromCodeCoverage]
public sealed class FavoriteProductsAdditionalFeatureTests {
    [Fact]
    public async Task AddFavoriteProduct_WithAccessibleProduct_PersistsAndReturnsModel() {
        var user = User.Create("favorite-product@example.com", "hash");
        Product product = CreateProduct(user.Id, "Greek Yogurt");
        var favoriteRepository = new InMemoryFavoriteProductRepository(product);
        var handler = new AddFavoriteProductCommandHandler(
            favoriteRepository,
            new SingleProductRepository(product),
            new SingleUserRepository(user));

        Result<FavoriteProductModel> result = await handler.Handle(
            new AddFavoriteProductCommand(user.Id.Value, product.Id.Value, "Breakfast", 125),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.NotNull(favoriteRepository.AddedFavorite);
        Assert.Equal(product.Id.Value, result.Value.ProductId);
        Assert.Equal("Greek Yogurt", result.Value.ProductName);
        Assert.Equal("Breakfast", result.Value.Name);
        Assert.Equal(125, result.Value.PreferredPortionAmount);
    }

    [Fact]
    public async Task AddFavoriteProduct_WhenProductMissing_ReturnsNotFound() {
        var user = User.Create("missing-favorite-product@example.com", "hash");
        var handler = new AddFavoriteProductCommandHandler(
            new InMemoryFavoriteProductRepository(),
            new SingleProductRepository(product: null),
            new SingleUserRepository(user));

        Result<FavoriteProductModel> result = await handler.Handle(
            new AddFavoriteProductCommand(user.Id.Value, Guid.NewGuid(), "Missing", PreferredPortionAmount: null),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Product.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task AddFavoriteProduct_WhenAlreadyExists_ReturnsFailure() {
        var user = User.Create("duplicate-favorite-product@example.com", "hash");
        Product product = CreateProduct(user.Id, "Apple");
        var existing = FavoriteProduct.Create(user.Id, product.Id, "Existing");
        SetProductNavigation(existing, product);
        var handler = new AddFavoriteProductCommandHandler(
            new InMemoryFavoriteProductRepository(product, [existing]),
            new SingleProductRepository(product),
            new SingleUserRepository(user));

        Result<FavoriteProductModel> result = await handler.Handle(
            new AddFavoriteProductCommand(user.Id.Value, product.Id.Value, "Again", PreferredPortionAmount: null),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("FavoriteProduct.AlreadyExists", result.Error.Code);
    }

    [Fact]
    public async Task AddFavoriteProduct_WithEmptyUserId_ReturnsInvalidToken() {
        var handler = new AddFavoriteProductCommandHandler(
            new InMemoryFavoriteProductRepository(),
            new SingleProductRepository(product: null),
            new SingleUserRepository(User.Create("invalid-add-favorite-product@example.com", "hash")));

        Result<FavoriteProductModel> result = await handler.Handle(
            new AddFavoriteProductCommand(Guid.Empty, Guid.NewGuid(), "Invalid", PreferredPortionAmount: null),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task AddFavoriteProduct_WhenUserMissing_ReturnsInvalidToken() {
        Product product = CreateProduct(UserId.New(), "Missing User Apple");
        var handler = new AddFavoriteProductCommandHandler(
            new InMemoryFavoriteProductRepository(product),
            new SingleProductRepository(product),
            new SingleUserRepository(user: null));

        Result<FavoriteProductModel> result = await handler.Handle(
            new AddFavoriteProductCommand(Guid.NewGuid(), product.Id.Value, "Snack", PreferredPortionAmount: null),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetFavoriteProducts_ReturnsMappedFavorites() {
        var user = User.Create("get-favorite-products@example.com", "hash");
        Product product = CreateProduct(user.Id, "Chicken");
        var favorite = FavoriteProduct.Create(user.Id, product.Id, "Lunch");
        SetProductNavigation(favorite, product);
        var handler = new GetFavoriteProductsQueryHandler(
            new InMemoryFavoriteProductRepository(product, [favorite]),
            new SingleUserRepository(user));

        Result<IReadOnlyList<FavoriteProductModel>> result = await handler.Handle(new GetFavoriteProductsQuery(user.Id.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Single(result.Value);
        Assert.Equal("Chicken", result.Value[0].ProductName);
        Assert.Equal(product.DefaultPortionAmount, result.Value[0].PreferredPortionAmount);
    }

    [Fact]
    public async Task GetFavoriteProducts_WithEmptyUserId_ReturnsInvalidToken() {
        var handler = new GetFavoriteProductsQueryHandler(
            new InMemoryFavoriteProductRepository(),
            new SingleUserRepository(User.Create("invalid-get-favorite-products@example.com", "hash")));

        Result<IReadOnlyList<FavoriteProductModel>> result = await handler.Handle(new GetFavoriteProductsQuery(Guid.Empty), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetFavoriteProducts_WhenUserMissing_ReturnsInvalidToken() {
        var handler = new GetFavoriteProductsQueryHandler(
            new InMemoryFavoriteProductRepository(),
            new SingleUserRepository(user: null));

        Result<IReadOnlyList<FavoriteProductModel>> result = await handler.Handle(new GetFavoriteProductsQuery(Guid.NewGuid()), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetFavoriteProducts_WhenUserDeleted_ReturnsAccessFailure() {
        var user = User.Create("deleted-get-favorite-products@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new GetFavoriteProductsQueryHandler(
            new InMemoryFavoriteProductRepository(),
            new SingleUserRepository(user));

        Result<IReadOnlyList<FavoriteProductModel>> result = await handler.Handle(new GetFavoriteProductsQuery(user.Id.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task IsProductFavorite_ReturnsFalseWhenFavoriteMissing() {
        var user = User.Create("is-favorite-product@example.com", "hash");
        var handler = new IsProductFavoriteQueryHandler(
            new InMemoryFavoriteProductRepository(),
            new SingleUserRepository(user));

        Result<bool> result = await handler.Handle(
            new IsProductFavoriteQuery(user.Id.Value, Guid.NewGuid()),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.False(result.Value);
    }

    [Fact]
    public async Task IsProductFavorite_WhenUserDeleted_ReturnsAccessFailure() {
        var user = User.Create("deleted-is-favorite-product@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new IsProductFavoriteQueryHandler(
            new InMemoryFavoriteProductRepository(),
            new SingleUserRepository(user));

        Result<bool> result = await handler.Handle(
            new IsProductFavoriteQuery(user.Id.Value, Guid.NewGuid()),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task IsProductFavorite_WithEmptyUserId_ReturnsInvalidToken() {
        var handler = new IsProductFavoriteQueryHandler(
            new InMemoryFavoriteProductRepository(),
            new SingleUserRepository(User.Create("invalid-is-favorite-product@example.com", "hash")));

        Result<bool> result = await handler.Handle(
            new IsProductFavoriteQuery(Guid.Empty, Guid.NewGuid()),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task IsProductFavorite_WhenUserMissing_ReturnsInvalidToken() {
        var handler = new IsProductFavoriteQueryHandler(
            new InMemoryFavoriteProductRepository(),
            new SingleUserRepository(user: null));

        Result<bool> result = await handler.Handle(
            new IsProductFavoriteQuery(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task RemoveFavoriteProduct_DeletesExistingFavorite() {
        var user = User.Create("remove-favorite-product@example.com", "hash");
        Product product = CreateProduct(user.Id, "Pear");
        var favorite = FavoriteProduct.Create(user.Id, product.Id, "Snack");
        SetProductNavigation(favorite, product);
        var repository = new InMemoryFavoriteProductRepository(product, [favorite]);
        var handler = new RemoveFavoriteProductCommandHandler(repository, new SingleUserRepository(user));

        Result result = await handler.Handle(
            new RemoveFavoriteProductCommand(user.Id.Value, favorite.Id.Value),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.True(repository.DeleteCalled);
    }

    [Fact]
    public async Task UpdateFavoriteProduct_WithExistingFavorite_UpdatesNameAndPreferredPortion() {
        var user = User.Create("update-favorite-product@example.com", "hash");
        Product product = CreateProduct(user.Id, "Cottage Cheese");
        var favorite = FavoriteProduct.Create(user.Id, product.Id, "Old name", 100);
        SetProductNavigation(favorite, product);
        var repository = new InMemoryFavoriteProductRepository(product, [favorite]);
        var handler = new UpdateFavoriteProductCommandHandler(repository, new SingleUserRepository(user));

        Result<FavoriteProductModel> result = await handler.Handle(
            new UpdateFavoriteProductCommand(user.Id.Value, favorite.Id.Value, "Evening snack", 180),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.True(repository.UpdateCalled);
        Assert.Equal("Evening snack", result.Value.Name);
        Assert.Equal(180, result.Value.PreferredPortionAmount);
    }

    [Fact]
    public async Task UpdateFavoriteProduct_WhenFavoriteMissing_ReturnsNotFound() {
        var user = User.Create("missing-update-favorite-product@example.com", "hash");
        var repository = new InMemoryFavoriteProductRepository();
        var handler = new UpdateFavoriteProductCommandHandler(repository, new SingleUserRepository(user));

        Result<FavoriteProductModel> result = await handler.Handle(
            new UpdateFavoriteProductCommand(user.Id.Value, Guid.NewGuid(), "Missing", 120),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("FavoriteProduct.NotFound", result.Error.Code);
        Assert.False(repository.UpdateCalled);
    }

    [Fact]
    public async Task UpdateFavoriteProduct_WithEmptyUserId_ReturnsInvalidToken() {
        var repository = new InMemoryFavoriteProductRepository();
        var handler = new UpdateFavoriteProductCommandHandler(
            repository,
            new SingleUserRepository(User.Create("invalid-update-favorite-product@example.com", "hash")));

        Result<FavoriteProductModel> result = await handler.Handle(
            new UpdateFavoriteProductCommand(Guid.Empty, Guid.NewGuid(), "Invalid", 120),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
        Assert.False(repository.UpdateCalled);
    }

    [Fact]
    public async Task UpdateFavoriteProduct_WhenUserDeleted_ReturnsAccountDeleted() {
        var user = User.Create("deleted-update-favorite-product@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        Product product = CreateProduct(user.Id, "Deleted User Pear");
        var favorite = FavoriteProduct.Create(user.Id, product.Id, "Snack");
        SetProductNavigation(favorite, product);
        var repository = new InMemoryFavoriteProductRepository(product, [favorite]);
        var handler = new UpdateFavoriteProductCommandHandler(repository, new SingleUserRepository(user));

        Result<FavoriteProductModel> result = await handler.Handle(
            new UpdateFavoriteProductCommand(user.Id.Value, favorite.Id.Value, "Updated", 120),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
        Assert.False(repository.UpdateCalled);
    }

    [Fact]
    public async Task RemoveFavoriteProduct_WhenFavoriteMissing_ReturnsNotFound() {
        var user = User.Create("missing-remove-favorite-product@example.com", "hash");
        var repository = new InMemoryFavoriteProductRepository();
        var handler = new RemoveFavoriteProductCommandHandler(repository, new SingleUserRepository(user));

        Result result = await handler.Handle(
            new RemoveFavoriteProductCommand(user.Id.Value, Guid.NewGuid()),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("FavoriteProduct.NotFound", result.Error.Code);
        Assert.False(repository.DeleteCalled);
    }

    [Fact]
    public async Task RemoveFavoriteProduct_WithEmptyUserId_ReturnsInvalidToken() {
        var repository = new InMemoryFavoriteProductRepository();
        var handler = new RemoveFavoriteProductCommandHandler(
            repository,
            new SingleUserRepository(User.Create("invalid-remove-favorite-product@example.com", "hash")));

        Result result = await handler.Handle(
            new RemoveFavoriteProductCommand(Guid.Empty, Guid.NewGuid()),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
        Assert.False(repository.DeleteCalled);
    }

    [Fact]
    public async Task RemoveFavoriteProduct_WhenUserMissing_ReturnsInvalidToken() {
        var userId = Guid.NewGuid();
        Product product = CreateProduct(new UserId(userId), "Missing User Pear");
        var favorite = FavoriteProduct.Create(new UserId(userId), product.Id, "Snack");
        SetProductNavigation(favorite, product);
        var repository = new InMemoryFavoriteProductRepository(product, [favorite]);
        var handler = new RemoveFavoriteProductCommandHandler(repository, new SingleUserRepository(user: null));

        Result result = await handler.Handle(
            new RemoveFavoriteProductCommand(userId, favorite.Id.Value),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
        Assert.False(repository.DeleteCalled);
    }

    private static Product CreateProduct(UserId userId, string name) =>
        Product.Create(
            userId,
            name,
            MeasurementUnit.G,
            baseAmount: 100,
            defaultPortionAmount: 100,
            caloriesPerBase: 50,
            proteinsPerBase: 1,
            fatsPerBase: 1,
            carbsPerBase: 10,
            fiberPerBase: 1,
            alcoholPerBase: 0,
            visibility: Visibility.Private);

    [ExcludeFromCodeCoverage]
    private sealed class InMemoryFavoriteProductRepository(
        Product? product = null,
        IReadOnlyList<FavoriteProduct>? favorites = null) : IFavoriteProductRepository {
        private readonly List<FavoriteProduct> _favorites = favorites?.ToList() ?? [];
        public FavoriteProduct? AddedFavorite { get; private set; }
        public bool DeleteCalled { get; private set; }
        public bool UpdateCalled { get; private set; }

        public Task<FavoriteProduct> AddAsync(FavoriteProduct favorite, CancellationToken cancellationToken = default) {
            if (product is not null) {
                SetProductNavigation(favorite, product);
            }

            AddedFavorite = favorite;
            _favorites.Add(favorite);
            return Task.FromResult(favorite);
        }

        public Task DeleteAsync(FavoriteProduct favorite, CancellationToken cancellationToken = default) {
            DeleteCalled = true;
            _favorites.Remove(favorite);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(FavoriteProduct favorite, CancellationToken cancellationToken = default) {
            UpdateCalled = true;
            return Task.CompletedTask;
        }

        public Task<FavoriteProduct?> GetByIdAsync(
            FavoriteProductId id,
            UserId userId,
            bool asTracking = false,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_favorites.FirstOrDefault(f => f.Id == id && f.UserId == userId));

        public Task<FavoriteProduct?> GetByProductIdAsync(
            ProductId productId,
            UserId userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_favorites.FirstOrDefault(f => f.ProductId == productId && f.UserId == userId));

        public Task<IReadOnlyList<FavoriteProduct>> GetAllAsync(UserId userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<FavoriteProduct>>(_favorites.Where(f => f.UserId == userId).ToList());

        public Task<IReadOnlyDictionary<ProductId, FavoriteProduct>> GetByProductIdsAsync(
            UserId userId,
            IReadOnlyCollection<ProductId> productIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<ProductId, FavoriteProduct>>(
                _favorites.Where(f => f.UserId == userId && productIds.Contains(f.ProductId)).ToDictionary(f => f.ProductId));
    }

    [ExcludeFromCodeCoverage]
    private sealed class SingleProductRepository(Product? product) : IProductRepository {
        public Task<Product> AddAsync(Product product, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<(Product Product, int UsageCount)> Items, int TotalItems)> GetPagedAsync(UserId userId, bool includePublic, int page, int limit, ProductQueryFilters filters, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Product?> GetByIdAsync(ProductId id, UserId userId, bool includePublic = true, CancellationToken cancellationToken = default) => Task.FromResult(product is not null && product.Id == id ? product : null);
        public Task<IReadOnlyDictionary<ProductId, Product>> GetByIdsAsync(IEnumerable<ProductId> ids, UserId userId, bool includePublic = true, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyDictionary<ProductId, (Product Product, int UsageCount)>> GetByIdsWithUsageAsync(IEnumerable<ProductId> ids, UserId userId, bool includePublic = true, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAsync(Product product, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAsync(Product product, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    [ExcludeFromCodeCoverage]
    private sealed class SingleUserRepository(User? user) : IUserRepository {
        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByEmailIncludingDeletedAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default) => Task.FromResult(user is not null && user.Id == id ? user : null);
        public Task<User?> GetByIdIncludingDeletedAsync(UserId id, CancellationToken cancellationToken = default) => Task.FromResult(user is not null && user.Id == id ? user : null);
        public Task<User?> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByTelegramUserIdIncludingDeletedAsync(long telegramUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<User> Items, int TotalItems)> GetPagedAsync(string? search, int page, int limit, bool includeDeleted, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<User> RecentUsers)> GetAdminDashboardSummaryAsync(int recentLimit, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Role>> GetRolesByNamesAsync(IReadOnlyList<string> names, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User> AddAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAsync(User user, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private static void SetProductNavigation(FavoriteProduct favorite, Product product) {
        typeof(FavoriteProduct)
            .GetProperty(nameof(FavoriteProduct.Product))!
            .SetValue(favorite, product);
    }
}
