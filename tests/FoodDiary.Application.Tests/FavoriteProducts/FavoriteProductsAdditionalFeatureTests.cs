using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.FavoriteProducts.Common;
using FoodDiary.Application.Abstractions.FavoriteProducts.Models;
using FoodDiary.Application.Abstractions.Products.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.FavoriteProducts.Commands.AddFavoriteProduct;
using FoodDiary.Application.FavoriteProducts.Commands.RemoveFavoriteProduct;
using FoodDiary.Application.FavoriteProducts.Commands.UpdateFavoriteProduct;
using FoodDiary.Application.FavoriteProducts.Common;
using FoodDiary.Application.FavoriteProducts.Mappings;
using FoodDiary.Application.FavoriteProducts.Models;
using FoodDiary.Application.FavoriteProducts.Queries.GetFavoriteProducts;
using FoodDiary.Application.FavoriteProducts.Queries.IsProductFavorite;
using FoodDiary.Application.FavoriteProducts.Services;
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
            CreateCurrentUserAccessService(user));

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
            CreateCurrentUserAccessService(user));

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
            CreateCurrentUserAccessService(user));

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
            CreateCurrentUserAccessService(User.Create("invalid-add-favorite-product@example.com", "hash")));

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
            CreateCurrentUserAccessService(user: null));

        Result<FavoriteProductModel> result = await handler.Handle(
            new AddFavoriteProductCommand(Guid.NewGuid(), product.Id.Value, "Snack", PreferredPortionAmount: null),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task AddFavoriteProduct_WithEmptyProductId_ReturnsValidationFailure() {
        var user = User.Create("empty-product-id@example.com", "hash");
        var handler = new AddFavoriteProductCommandHandler(
            new InMemoryFavoriteProductRepository(),
            new SingleProductRepository(product: null),
            CreateCurrentUserAccessService(user));

        Result<FavoriteProductModel> result = await handler.Handle(
            new AddFavoriteProductCommand(user.Id.Value, Guid.Empty, "Invalid", PreferredPortionAmount: null),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("ProductId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetFavoriteProducts_ReturnsMappedFavorites() {
        var user = User.Create("get-favorite-products@example.com", "hash");
        Product product = CreateProduct(user.Id, "Chicken");
        var favorite = FavoriteProduct.Create(user.Id, product.Id, "Lunch");
        SetProductNavigation(favorite, product);
        var handler = new GetFavoriteProductsQueryHandler(
            new InMemoryFavoriteProductRepository(product, [favorite]),
            CreateCurrentUserAccessService(user));

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
            CreateCurrentUserAccessService(User.Create("invalid-get-favorite-products@example.com", "hash")));

        Result<IReadOnlyList<FavoriteProductModel>> result = await handler.Handle(new GetFavoriteProductsQuery(Guid.Empty), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetFavoriteProducts_WhenUserMissing_ReturnsInvalidToken() {
        var handler = new GetFavoriteProductsQueryHandler(
            new InMemoryFavoriteProductRepository(),
            CreateCurrentUserAccessService(user: null));

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
            CreateCurrentUserAccessService(user));

        Result<IReadOnlyList<FavoriteProductModel>> result = await handler.Handle(new GetFavoriteProductsQuery(user.Id.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task IsProductFavorite_ReturnsFalseWhenFavoriteMissing() {
        var user = User.Create("is-favorite-product@example.com", "hash");
        var handler = new IsProductFavoriteQueryHandler(
            new InMemoryFavoriteProductRepository(),
            CreateCurrentUserAccessService(user));

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
            CreateCurrentUserAccessService(user));

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
            CreateCurrentUserAccessService(User.Create("invalid-is-favorite-product@example.com", "hash")));

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
            CreateCurrentUserAccessService(user: null));

        Result<bool> result = await handler.Handle(
            new IsProductFavoriteQuery(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task IsProductFavorite_WithEmptyProductId_ReturnsValidationFailure() {
        var user = User.Create("is-empty-product-id@example.com", "hash");
        var handler = new IsProductFavoriteQueryHandler(
            new InMemoryFavoriteProductRepository(),
            CreateCurrentUserAccessService(user));

        Result<bool> result = await handler.Handle(
            new IsProductFavoriteQuery(user.Id.Value, Guid.Empty),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("ProductId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RemoveFavoriteProduct_DeletesExistingFavorite() {
        var user = User.Create("remove-favorite-product@example.com", "hash");
        Product product = CreateProduct(user.Id, "Pear");
        var favorite = FavoriteProduct.Create(user.Id, product.Id, "Snack");
        SetProductNavigation(favorite, product);
        var repository = new InMemoryFavoriteProductRepository(product, [favorite]);
        var handler = new RemoveFavoriteProductCommandHandler(repository, CreateCurrentUserAccessService(user));

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
        var handler = new UpdateFavoriteProductCommandHandler(repository, CreateCurrentUserAccessService(user));

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
        var handler = new UpdateFavoriteProductCommandHandler(repository, CreateCurrentUserAccessService(user));

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
            CreateCurrentUserAccessService(User.Create("invalid-update-favorite-product@example.com", "hash")));

        Result<FavoriteProductModel> result = await handler.Handle(
            new UpdateFavoriteProductCommand(Guid.Empty, Guid.NewGuid(), "Invalid", 120),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
        Assert.False(repository.UpdateCalled);
    }

    [Fact]
    public async Task UpdateFavoriteProduct_WithEmptyFavoriteProductId_ReturnsValidationFailure() {
        var user = User.Create("update-empty-favorite-product-id@example.com", "hash");
        var repository = new InMemoryFavoriteProductRepository();
        var handler = new UpdateFavoriteProductCommandHandler(
            repository,
            CreateCurrentUserAccessService(user));

        Result<FavoriteProductModel> result = await handler.Handle(
            new UpdateFavoriteProductCommand(user.Id.Value, Guid.Empty, "Invalid", 120),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("FavoriteProductId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
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
        var handler = new UpdateFavoriteProductCommandHandler(repository, CreateCurrentUserAccessService(user));

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
        var handler = new RemoveFavoriteProductCommandHandler(repository, CreateCurrentUserAccessService(user));

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
            CreateCurrentUserAccessService(User.Create("invalid-remove-favorite-product@example.com", "hash")));

        Result result = await handler.Handle(
            new RemoveFavoriteProductCommand(Guid.Empty, Guid.NewGuid()),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
        Assert.False(repository.DeleteCalled);
    }

    [Fact]
    public async Task RemoveFavoriteProduct_WithEmptyFavoriteProductId_ReturnsValidationFailure() {
        var user = User.Create("remove-empty-favorite-product-id@example.com", "hash");
        var repository = new InMemoryFavoriteProductRepository();
        var handler = new RemoveFavoriteProductCommandHandler(
            repository,
            CreateCurrentUserAccessService(user));

        Result result = await handler.Handle(
            new RemoveFavoriteProductCommand(user.Id.Value, Guid.Empty),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("FavoriteProductId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(repository.DeleteCalled);
    }

    [Fact]
    public async Task RemoveFavoriteProduct_WhenUserMissing_ReturnsInvalidToken() {
        var userId = Guid.NewGuid();
        Product product = CreateProduct(new UserId(userId), "Missing User Pear");
        var favorite = FavoriteProduct.Create(new UserId(userId), product.Id, "Snack");
        SetProductNavigation(favorite, product);
        var repository = new InMemoryFavoriteProductRepository(product, [favorite]);
        var handler = new RemoveFavoriteProductCommandHandler(repository, CreateCurrentUserAccessService(user: null));

        Result result = await handler.Handle(
            new RemoveFavoriteProductCommand(userId, favorite.Id.Value),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
        Assert.False(repository.DeleteCalled);
    }

    [Fact]
    public async Task FavoriteProductReadService_ExistsByProductIdAsync_DelegatesToReadRepository() {
        var userId = UserId.New();
        Product product = CreateProduct(userId, "Favorite Read Product");
        var favorite = FavoriteProduct.Create(userId, product.Id, "Snack", preferredPortionAmount: 80);
        SetProductNavigation(favorite, product);
        var repository = new InMemoryFavoriteProductRepository(product, [favorite]);
        var service = new FavoriteProductReadService(repository);

        bool exists = await service.ExistsByProductIdAsync(product.Id, userId, CancellationToken.None);

        Assert.True(exists);
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
        IReadOnlyList<FavoriteProduct>? favorites = null) : IFavoriteProductRepository, IFavoriteProductReadService, IFavoriteProductReadModelRepository {
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

        public Task<bool> ExistsByProductIdAsync(
            ProductId productId,
            UserId userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_favorites.Any(f => f.ProductId == productId && f.UserId == userId));

        public Task<IReadOnlyList<FavoriteProduct>> GetAllAsync(UserId userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<FavoriteProduct>>(_favorites.Where(f => f.UserId == userId).ToList());

        public Task<IReadOnlyList<FavoriteProductReadModel>> GetAllReadModelsAsync(UserId userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<FavoriteProductReadModel>>([.. _favorites.Where(f => f.UserId == userId).Select(ToReadModel)]);

        public Task<IReadOnlyDictionary<ProductId, FavoriteProduct>> GetByProductIdsAsync(
            UserId userId,
            IReadOnlyCollection<ProductId> productIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<ProductId, FavoriteProduct>>(
                _favorites.Where(f => f.UserId == userId && productIds.Contains(f.ProductId)).ToDictionary(f => f.ProductId));

        private static FavoriteProductReadModel ToReadModel(FavoriteProduct favorite) =>
            new(
                favorite.Id.Value,
                favorite.ProductId.Value,
                favorite.UserId.Value,
                favorite.Name,
                favorite.CreatedAtUtc,
                favorite.Product.Name,
                favorite.Product.Brand,
                favorite.Product.Barcode,
                favorite.Product.UserId == favorite.UserId ? favorite.Product.Comment : null,
                favorite.Product.ImageUrl,
                favorite.Product.CaloriesPerBase,
                favorite.Product.ProteinsPerBase,
                favorite.Product.FatsPerBase,
                favorite.Product.CarbsPerBase,
                favorite.Product.FiberPerBase,
                favorite.Product.AlcoholPerBase,
                favorite.Product.ProductType,
                favorite.Product.BaseUnit,
                favorite.PreferredPortionAmount,
                favorite.Product.DefaultPortionAmount,
                favorite.Product.UserId.Value);
        async Task<IReadOnlyList<FavoriteProductModel>> IFavoriteProductReadService.GetAllAsync(
            UserId userId,
            CancellationToken cancellationToken) {
            IReadOnlyList<FavoriteProduct> favoriteEntities = await GetAllAsync(userId, cancellationToken).ConfigureAwait(false);
            return [.. favoriteEntities.Select(favorite => favorite.ToModel())];
        }

        async Task<bool> IFavoriteProductReadService.ExistsByProductIdAsync(
            ProductId productId,
            UserId userId,
            CancellationToken cancellationToken) =>
            await GetByProductIdAsync(productId, userId, cancellationToken).ConfigureAwait(false) is not null;
    }

    [ExcludeFromCodeCoverage]
    private sealed class SingleProductRepository(Product? product) : IProductLookupService {
        public Task<IReadOnlyDictionary<ProductId, Product>> GetAccessibleByIdsAsync(
            IEnumerable<ProductId> ids,
            UserId userId,
            CancellationToken cancellationToken = default) {
            IReadOnlyDictionary<ProductId, Product> products = product is not null && ids.Contains(product.Id)
                ? new[] { product }.ToDictionary(item => item.Id)
                : Array.Empty<Product>().ToDictionary(item => item.Id);
            return Task.FromResult(products);
        }
    }

    private static ICurrentUserAccessService CreateCurrentUserAccessService(User? user) {
        ICurrentUserAccessService service = Substitute.For<ICurrentUserAccessService>();
        service
            .EnsureCanAccessAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                UserId userId = call.Arg<UserId>();
                Error? error = user switch {
                    null => Errors.Authentication.InvalidToken,
                    { Id: var id } when id != userId => Errors.Authentication.InvalidToken,
                    { DeletedAt: not null } => Errors.Authentication.AccountDeleted,
                    _ => null,
                };
                return Task.FromResult(error);
            });

        return service;
    }

    private static void SetProductNavigation(FavoriteProduct favorite, Product product) {
        typeof(FavoriteProduct)
            .GetProperty(nameof(FavoriteProduct.Product))!
            .SetValue(favorite, product);
    }
}
