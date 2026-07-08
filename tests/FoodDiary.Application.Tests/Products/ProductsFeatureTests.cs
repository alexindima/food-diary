using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.FavoriteProducts.Common;
using FoodDiary.Application.Abstractions.FavoriteProducts.Models;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Application.Abstractions.Products.Common;
using FoodDiary.Application.Abstractions.Products.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Products.Commands.CreateProduct;
using FoodDiary.Application.Products.Commands.UpdateProduct;
using FoodDiary.Application.FavoriteProducts.Services;
using FoodDiary.Application.Products.Mappings;
using FoodDiary.Application.Products.Queries.GetProductsOverview;
using FoodDiary.Application.Products.Queries.GetRecentProducts;
using FoodDiary.Application.Abstractions.RecentItems.Common;
using FoodDiary.Application.Products.Services;
using FoodDiary.Domain.Entities.FavoriteProducts;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Products.Models;

namespace FoodDiary.Application.Tests.Products;

[ExcludeFromCodeCoverage]
public partial class ProductsFeatureTests {

    private static GetProductsOverviewQueryHandler CreateProductsOverviewHandler(
        IProductOverviewReadService overviewReadService,
        StubRecentItemRepository recentRepository,
        StubFavoriteProductRepository favoriteRepository,
        ICurrentUserAccessService currentUserAccessService) =>
        new(
            overviewReadService,
            new RecentProductReadService(recentRepository, overviewReadService),
            new FavoriteProductReadService(favoriteRepository),
            currentUserAccessService);

    private static GetRecentProductsQueryHandler CreateRecentProductsHandler(
        StubRecentItemRepository recentRepository,
        IProductOverviewReadService overviewReadService) =>
        new(new RecentProductReadService(recentRepository, overviewReadService), Substitute.For<ICurrentUserAccessService>());

    [ExcludeFromCodeCoverage]
    private sealed class NoopProductRepository : IProductRepository {
        public Product? LastAddedProduct { get; private set; }

        public Task<Product> AddAsync(Product product, CancellationToken cancellationToken = default) {
            LastAddedProduct = product;
            return Task.FromResult(product);
        }

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

        public Task<int> GetUsageCountAsync(
            ProductId id,
            UserId userId,
            bool includePublic = true,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

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

        public Task<int> GetUsageCountAsync(
            ProductId id,
            UserId userId,
            bool includePublic = true,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(product.MealItems.Count + product.RecipeIngredients.Count);

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
    private sealed class OverviewProductReadService(
        IReadOnlyList<(Product Product, int UsageCount)>? pagedItems = null,
        IReadOnlyDictionary<ProductId, (Product Product, int UsageCount)>? productsByIdWithUsage = null) : IProductOverviewReadService {
        private readonly IReadOnlyList<(Product Product, int UsageCount)> _pagedItems = pagedItems ?? [];
        private readonly IReadOnlyDictionary<ProductId, (Product Product, int UsageCount)> _productsByIdWithUsage = productsByIdWithUsage ?? new Dictionary<ProductId, (Product Product, int UsageCount)>();

        public Task<(IReadOnlyList<ProductOverviewReadItem> Items, int TotalItems)> GetPagedAsync(
            UserId userId,
            bool includePublic,
            int page,
            int limit,
            ProductQueryFilters filters,
            CancellationToken cancellationToken = default) {
            var filtered = _pagedItems
                .Where(item => filters.ProductTypes?.Contains(item.Product.ProductType) != false)
                .Select(item => ToReadItem(item.Product, item.UsageCount, userId))
                .ToList();
            return Task.FromResult(((IReadOnlyList<ProductOverviewReadItem>)filtered, filtered.Count));
        }

        public Task<IReadOnlyDictionary<ProductId, ProductOverviewReadItem>> GetByIdsWithUsageAsync(
            IEnumerable<ProductId> ids,
            UserId userId,
            bool includePublic = true,
            CancellationToken cancellationToken = default) {
            var idSet = ids.ToHashSet();
            var filtered = _productsByIdWithUsage
                .Where(pair => idSet.Contains(pair.Key))
                .ToDictionary(pair => pair.Key, pair => ToReadItem(pair.Value.Product, pair.Value.UsageCount, userId));
            return Task.FromResult<IReadOnlyDictionary<ProductId, ProductOverviewReadItem>>(filtered);
        }

        private static ProductOverviewReadItem ToReadItem(Product product, int usageCount, UserId userId) {
            ProductModel model = product.ToModel(usageCount, product.UserId == userId);
            return new ProductOverviewReadItem(
                product.Id,
                product.UserId,
                model.Barcode,
                model.Name,
                model.Brand,
                product.ProductType,
                model.Category,
                model.Description,
                model.Comment,
                model.ImageUrl,
                product.ImageAssetId,
                product.BaseUnit,
                model.BaseAmount,
                model.DefaultPortionAmount,
                model.CaloriesPerBase,
                model.ProteinsPerBase,
                model.FatsPerBase,
                model.CarbsPerBase,
                model.FiberPerBase,
                model.AlcoholPerBase,
                model.UsageCount,
                product.Visibility,
                model.CreatedAt,
                model.IsOwnedByCurrentUser,
                model.QualityScore,
                model.QualityGrade,
                model.UsdaFdcId);
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class OverviewProductRepository(
        IReadOnlyList<(Product Product, int UsageCount)>? pagedItems = null,
        IReadOnlyDictionary<ProductId, (Product Product, int UsageCount)>? productsByIdWithUsage = null) : IProductRepository {
        private readonly IReadOnlyList<(Product Product, int UsageCount)> _pagedItems = pagedItems ?? [];
        private readonly IReadOnlyDictionary<ProductId, (Product Product, int UsageCount)> _productsByIdWithUsage = productsByIdWithUsage ?? new Dictionary<ProductId, (Product Product, int UsageCount)>();

        public Task<Product> AddAsync(Product product, CancellationToken cancellationToken = default) => throw new NotSupportedException();

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

        public Task<int> GetUsageCountAsync(
            ProductId id,
            UserId userId,
            bool includePublic = true,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_pagedItems.FirstOrDefault(item => item.Product.Id == id).UsageCount);

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
        public Task<bool> ExistsByProductIdAsync(ProductId productId, UserId userId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyDictionary<ProductId, FavoriteProduct>> GetByProductIdsAsync(UserId userId, IReadOnlyCollection<ProductId> productIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<ProductId, FavoriteProduct>>(_favorites.Where(f => productIds.Contains(f.ProductId)).ToDictionary(f => f.ProductId));
        public Task<IReadOnlyList<FavoriteProduct>> GetAllAsync(UserId userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_favorites);
        public Task<IReadOnlyList<FavoriteProductReadModel>> GetAllReadModelsAsync(UserId userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<FavoriteProductReadModel>>([.. _favorites.Select(ToReadModel)]);

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
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubUserRepository(User user) : ICurrentUserAccessService {
        public Task<Error?> EnsureCanAccessAsync(UserId userId, CancellationToken cancellationToken = default) {
            Error? error = user switch {
                { DeletedAt: not null } => Errors.Authentication.AccountDeleted,
                { IsActive: false } => Errors.Authentication.InvalidToken,
                _ => null,
            };
            return Task.FromResult(error);
        }
    }

    private static void SetFavoriteProductNavigation(FavoriteProduct favorite, Product product) {
        typeof(FavoriteProduct)
            .GetProperty(nameof(FavoriteProduct.Product))!
            .SetValue(favorite, product);
    }

    private static Product CreateProduct(UserId userId, string name, string? imageUrl = null) =>
        Product.Create(
            userId,
            name,
            MeasurementUnit.G,
            baseAmount: 100,
            defaultPortionAmount: 100,
            caloriesPerBase: 80,
            proteinsPerBase: 5,
            fatsPerBase: 2,
            carbsPerBase: 10,
            fiberPerBase: 1,
            alcoholPerBase: 0,
            imageUrl: imageUrl,
            visibility: Visibility.Private);

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
