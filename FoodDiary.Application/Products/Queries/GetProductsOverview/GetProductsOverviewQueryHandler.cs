using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Abstractions.FavoriteProducts.Common;
using FoodDiary.Application.FavoriteProducts.Mappings;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Products.Mappings;
using FoodDiary.Application.Products.Models;
using FoodDiary.Application.Abstractions.RecentItems.Common;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using Product = FoodDiary.Domain.Entities.Products.Product;
using FoodDiary.Domain.Entities.FavoriteProducts;

namespace FoodDiary.Application.Products.Queries.GetProductsOverview;

public sealed class GetProductsOverviewQueryHandler(
    IProductRepository productRepository,
    IRecentItemRepository recentItemRepository,
    IFavoriteProductRepository favoriteProductRepository)
    : IQueryHandler<GetProductsOverviewQuery, Result<ProductOverviewModel>> {
    private sealed record ProductOverviewOptions(
        UserId UserId,
        int PageNumber,
        int PageSize,
        int RecentLimit,
        int FavoriteLimit,
        ProductType[]? ProductTypes,
        HashSet<ProductType>? SelectedProductTypes);

    private sealed record ProductListItem(
        Product Product,
        int UsageCount,
        bool IsOwner);

    public async Task<Result<ProductOverviewModel>> Handle(
        GetProductsOverviewQuery query,
        CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<ProductOverviewModel>(Errors.Authentication.InvalidToken);
        }

        ProductOverviewOptions options = CreateOptions(query);

        (IReadOnlyList<(Product Product, int UsageCount)>? items, int totalItems) = await productRepository.GetPagedAsync(
            options.UserId,
            query.IncludePublic,
            options.PageNumber,
            options.PageSize,
            query.Search,
            options.ProductTypes,
            cancellationToken).ConfigureAwait(false);

        var allProducts = items
            .Select(item => ToListItem(item.Product, item.UsageCount, options.UserId))
            .ToList();
        IReadOnlyList<FavoriteProduct> allFavorites = await favoriteProductRepository.GetAllAsync(options.UserId, cancellationToken).ConfigureAwait(false);
        var favoriteItems = allFavorites
            .Take(options.FavoriteLimit)
            .Select(favorite => favorite.ToModel())
            .ToList();
        var favoriteLookup = allFavorites.ToDictionary(favorite => favorite.ProductId);

        IReadOnlyList<ProductListItem> recentItems = await GetRecentItemsAsync(query, options, cancellationToken).ConfigureAwait(false);
        ProductId[] favoriteProductIds = [.. allProducts
            .Select(x => x.Product.Id)
            .Concat(recentItems.Select(x => x.Product.Id))
            .Distinct()];
        var favoritesByProductId = favoriteLookup
            .Where(pair => favoriteProductIds.Contains(pair.Key))
            .ToDictionary();

        PagedResponse<ProductModel> allPaged = CreatePagedProducts(
            allProducts,
            favoritesByProductId,
            options,
            totalItems);
        ProductModel[] recentResponses = ToProductModels(recentItems, favoritesByProductId);

        return Result.Success(new ProductOverviewModel(recentResponses, allPaged, favoriteItems, allFavorites.Count));
    }

    private static ProductOverviewOptions CreateOptions(GetProductsOverviewQuery query) {
        ProductType[]? productTypes = query.ProductTypes?
            .Select(type => Enum.TryParse<ProductType>(type, ignoreCase: true, out ProductType parsed) ? parsed : (ProductType?)null)
            .OfType<ProductType>()
            .Distinct()
            .ToArray();

        return new ProductOverviewOptions(
            new UserId(query.UserId!.Value),
            Math.Max(query.Page, 1),
            Math.Max(query.Limit, 1),
            Math.Clamp(query.RecentLimit, 1, 50),
            Math.Clamp(query.FavoriteLimit, 1, 50),
            productTypes is { Length: > 0 } ? productTypes : null,
            productTypes is { Length: > 0 } ? [.. productTypes] : null);
    }

    private async Task<IReadOnlyList<ProductListItem>> GetRecentItemsAsync(
        GetProductsOverviewQuery query,
        ProductOverviewOptions options,
        CancellationToken cancellationToken) {
        if (!string.IsNullOrWhiteSpace(query.Search)) {
            return [];
        }

        IReadOnlyList<RecentProductUsage> recents = await recentItemRepository.GetRecentProductsAsync(
            options.UserId,
            options.RecentLimit,
            cancellationToken).ConfigureAwait(false);
        if (recents.Count == 0) {
            return [];
        }

        var recentIds = recents.Select(x => x.ProductId).ToList();
        IReadOnlyDictionary<ProductId, (Product Product, int UsageCount)> productsById = await productRepository.GetByIdsWithUsageAsync(
            recentIds,
            options.UserId,
            query.IncludePublic,
            cancellationToken).ConfigureAwait(false);

        return recentIds
            .Where(productsById.ContainsKey)
            .Select(id => productsById[id])
            .Where(item => IsSelectedProductType(item.Product, options.SelectedProductTypes))
            .Select(item => ToListItem(item.Product, item.UsageCount, options.UserId))
            .ToArray();
    }

    private static ProductListItem ToListItem(Product product, int usageCount, UserId userId) =>
        new(product, usageCount, product.UserId == userId);

    private static bool IsSelectedProductType(
        Product product,
        IReadOnlySet<ProductType>? selectedProductTypes) =>
        selectedProductTypes is null || selectedProductTypes.Contains(product.ProductType);

    private static PagedResponse<ProductModel> CreatePagedProducts(
        IReadOnlyList<ProductListItem> products,
        IReadOnlyDictionary<ProductId, Domain.Entities.FavoriteProducts.FavoriteProduct> favoritesByProductId,
        ProductOverviewOptions options,
        int totalItems) =>
        new(
            ToProductModels(products, favoritesByProductId).ToList(),
            options.PageNumber,
            options.PageSize,
            (int)Math.Ceiling(totalItems / (double)options.PageSize),
            totalItems);

    private static ProductModel[] ToProductModels(
        IEnumerable<ProductListItem> products,
        IReadOnlyDictionary<ProductId, Domain.Entities.FavoriteProducts.FavoriteProduct> favoritesByProductId) =>
        [.. products.Select(product => ToProductModel(product, favoritesByProductId))];

    private static ProductModel ToProductModel(
        ProductListItem product,
        IReadOnlyDictionary<ProductId, Domain.Entities.FavoriteProducts.FavoriteProduct> favoritesByProductId) {
        FavoriteProduct? favorite = favoritesByProductId.GetValueOrDefault(product.Product.Id);
        return product.Product.ToModel(
            product.UsageCount,
            product.IsOwner,
            favorite is not null,
            favorite?.Id.Value);
    }
}
