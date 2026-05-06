using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.FavoriteProducts.Common;
using FoodDiary.Application.FavoriteProducts.Commands.AddFavoriteProduct;
using FoodDiary.Application.FavoriteProducts.Commands.RemoveFavoriteProduct;
using FoodDiary.Application.FavoriteProducts.Queries.GetFavoriteProducts;
using FoodDiary.Application.FavoriteProducts.Queries.IsProductFavorite;
using FoodDiary.Domain.Entities.FavoriteProducts;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.FavoriteProducts;

public sealed class FavoriteProductsAdditionalFeatureTests {
    [Fact]
    public async Task AddFavoriteProduct_WithAccessibleProduct_PersistsAndReturnsModel() {
        var user = User.Create("favorite-product@example.com", "hash");
        var product = CreateProduct(user.Id, "Greek Yogurt");
        var favoriteRepository = new InMemoryFavoriteProductRepository(product);
        var handler = new AddFavoriteProductCommandHandler(
            favoriteRepository,
            new SingleProductRepository(product),
            new SingleUserRepository(user));

        var result = await handler.Handle(
            new AddFavoriteProductCommand(user.Id.Value, product.Id.Value, "Breakfast"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(favoriteRepository.AddedFavorite);
        Assert.Equal(product.Id.Value, result.Value.ProductId);
        Assert.Equal("Greek Yogurt", result.Value.ProductName);
        Assert.Equal("Breakfast", result.Value.Name);
    }

    [Fact]
    public async Task AddFavoriteProduct_WhenProductMissing_ReturnsNotFound() {
        var user = User.Create("missing-favorite-product@example.com", "hash");
        var handler = new AddFavoriteProductCommandHandler(
            new InMemoryFavoriteProductRepository(),
            new SingleProductRepository(null),
            new SingleUserRepository(user));

        var result = await handler.Handle(
            new AddFavoriteProductCommand(user.Id.Value, Guid.NewGuid(), "Missing"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Product.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task AddFavoriteProduct_WhenAlreadyExists_ReturnsFailure() {
        var user = User.Create("duplicate-favorite-product@example.com", "hash");
        var product = CreateProduct(user.Id, "Apple");
        var existing = FavoriteProduct.Create(user.Id, product.Id, "Existing");
        SetProductNavigation(existing, product);
        var handler = new AddFavoriteProductCommandHandler(
            new InMemoryFavoriteProductRepository(product, [existing]),
            new SingleProductRepository(product),
            new SingleUserRepository(user));

        var result = await handler.Handle(
            new AddFavoriteProductCommand(user.Id.Value, product.Id.Value, "Again"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("FavoriteProduct.AlreadyExists", result.Error.Code);
    }

    [Fact]
    public async Task GetFavoriteProducts_ReturnsMappedFavorites() {
        var user = User.Create("get-favorite-products@example.com", "hash");
        var product = CreateProduct(user.Id, "Chicken");
        var favorite = FavoriteProduct.Create(user.Id, product.Id, "Lunch");
        SetProductNavigation(favorite, product);
        var handler = new GetFavoriteProductsQueryHandler(
            new InMemoryFavoriteProductRepository(product, [favorite]),
            new SingleUserRepository(user));

        var result = await handler.Handle(new GetFavoriteProductsQuery(user.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.Equal("Chicken", result.Value[0].ProductName);
    }

    [Fact]
    public async Task IsProductFavorite_ReturnsFalseWhenFavoriteMissing() {
        var user = User.Create("is-favorite-product@example.com", "hash");
        var handler = new IsProductFavoriteQueryHandler(
            new InMemoryFavoriteProductRepository(),
            new SingleUserRepository(user));

        var result = await handler.Handle(
            new IsProductFavoriteQuery(user.Id.Value, Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value);
    }

    [Fact]
    public async Task RemoveFavoriteProduct_DeletesExistingFavorite() {
        var user = User.Create("remove-favorite-product@example.com", "hash");
        var product = CreateProduct(user.Id, "Pear");
        var favorite = FavoriteProduct.Create(user.Id, product.Id, "Snack");
        SetProductNavigation(favorite, product);
        var repository = new InMemoryFavoriteProductRepository(product, [favorite]);
        var handler = new RemoveFavoriteProductCommandHandler(repository, new SingleUserRepository(user));

        var result = await handler.Handle(
            new RemoveFavoriteProductCommand(user.Id.Value, favorite.Id.Value),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(repository.DeleteCalled);
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

    private sealed class InMemoryFavoriteProductRepository(
        Product? product = null,
        IReadOnlyList<FavoriteProduct>? favorites = null) : IFavoriteProductRepository {
        private readonly List<FavoriteProduct> _favorites = favorites?.ToList() ?? [];
        public FavoriteProduct? AddedFavorite { get; private set; }
        public bool DeleteCalled { get; private set; }

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

    private sealed class SingleProductRepository(Product? product) : IProductRepository {
        public Task<Product> AddAsync(Product product, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<(Product Product, int UsageCount)> Items, int TotalItems)> GetPagedAsync(UserId userId, bool includePublic, int page, int limit, string? search, IReadOnlyCollection<ProductType>? productTypes = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Product?> GetByIdAsync(ProductId id, UserId userId, bool includePublic = true, CancellationToken cancellationToken = default) => Task.FromResult(product is not null && product.Id == id ? product : null);
        public Task<IReadOnlyDictionary<ProductId, Product>> GetByIdsAsync(IEnumerable<ProductId> ids, UserId userId, bool includePublic = true, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyDictionary<ProductId, (Product Product, int UsageCount)>> GetByIdsWithUsageAsync(IEnumerable<ProductId> ids, UserId userId, bool includePublic = true, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAsync(Product product, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAsync(Product product, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class SingleUserRepository(User user) : IUserRepository {
        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByEmailIncludingDeletedAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default) => Task.FromResult<User?>(user.Id == id ? user : null);
        public Task<User?> GetByIdIncludingDeletedAsync(UserId id, CancellationToken cancellationToken = default) => Task.FromResult<User?>(user.Id == id ? user : null);
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
