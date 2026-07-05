using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Abstractions.FavoriteProducts.Common;
using FoodDiary.Application.Abstractions.Products.Common;
using FoodDiary.Application.Abstractions.Products.Models;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.FavoriteProducts.Mappings;
using FoodDiary.Application.Products.Mappings;
using FoodDiary.Application.Products.Models;
using FoodDiary.Application.Abstractions.RecentItems.Common;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.FavoriteProducts;

namespace FoodDiary.Application.Products.Queries.GetProductsOverview;

public sealed class GetProductsOverviewQueryHandler(
    IProductOverviewReadService productOverviewReadService,
    IRecentItemReadRepository recentItemRepository,
    IFavoriteProductReadRepository favoriteProductRepository,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetProductsOverviewQuery, Result<ProductOverviewModel>> {
    private sealed record ProductOverviewOptions(
        UserId UserId,
        int PageNumber,
        int PageSize,
        int RecentLimit,
        int FavoriteLimit,
        ProductType[]? ProductTypes,
        HashSet<ProductType>? SelectedProductTypes,
        double? CaloriesFrom,
        double? CaloriesTo,
        bool? HasImage);

    public async Task<Result<ProductOverviewModel>> Handle(
        GetProductsOverviewQuery query,
        CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<ProductOverviewModel>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(query.UserId.Value);
        Error? accessError = await currentUserAccessService.EnsureCanAccessAsync(userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<ProductOverviewModel>(accessError);
        }

        ProductOverviewOptions options = CreateOptions(query, userId);

        (IReadOnlyList<ProductOverviewReadItem> items, int totalItems) = await productOverviewReadService.GetPagedAsync(
            options.UserId,
            query.IncludePublic,
            options.PageNumber,
            options.PageSize,
            new ProductQueryFilters(
                query.Search,
                options.ProductTypes,
                options.CaloriesFrom,
                options.CaloriesTo,
                options.HasImage),
            cancellationToken).ConfigureAwait(false);

        var allProducts = items.ToList();
        IReadOnlyList<FavoriteProduct> allFavorites = await favoriteProductRepository.GetAllAsync(options.UserId, cancellationToken).ConfigureAwait(false);
        var favoriteItems = allFavorites
            .Take(options.FavoriteLimit)
            .Select(favorite => favorite.ToModel())
            .ToList();
        var favoriteLookup = allFavorites.ToDictionary(favorite => favorite.ProductId);

        IReadOnlyList<ProductOverviewReadItem> recentItems = await GetRecentItemsAsync(query, options, cancellationToken).ConfigureAwait(false);
        ProductId[] favoriteProductIds = [.. allProducts
            .Select(x => x.Id)
            .Concat(recentItems.Select(x => x.Id))
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

    private static ProductOverviewOptions CreateOptions(GetProductsOverviewQuery query, UserId userId) {
        ProductType[]? productTypes = EnumFilterParser.ParseMany<ProductType>(query.ProductTypes);

        return new ProductOverviewOptions(
            userId,
            Math.Max(query.Page, 1),
            Math.Max(query.Limit, 1),
            Math.Clamp(query.RecentLimit, 1, 50),
            Math.Clamp(query.FavoriteLimit, 1, 50),
            productTypes is { Length: > 0 } ? productTypes : null,
            productTypes is { Length: > 0 } ? [.. productTypes] : null,
            query.CaloriesFrom,
            query.CaloriesTo,
            query.HasImage);
    }

    private async Task<IReadOnlyList<ProductOverviewReadItem>> GetRecentItemsAsync(
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
        IReadOnlyDictionary<ProductId, ProductOverviewReadItem> productsById = await productOverviewReadService.GetByIdsWithUsageAsync(
            recentIds,
            options.UserId,
            query.IncludePublic,
            cancellationToken).ConfigureAwait(false);

        return recentIds
            .Where(productsById.ContainsKey)
            .Select(id => productsById[id])
            .Where(item => MatchesRecentFilters(item, options))
            .ToArray();
    }

    private static bool IsSelectedProductType(
        ProductOverviewReadItem product,
        IReadOnlySet<ProductType>? selectedProductTypes) =>
        selectedProductTypes?.Contains(product.ProductType) != false;

    private static bool MatchesRecentFilters(ProductOverviewReadItem product, ProductOverviewOptions options) =>
        IsSelectedProductType(product, options.SelectedProductTypes) &&
        (!options.CaloriesFrom.HasValue || product.CaloriesPerBase >= options.CaloriesFrom.Value) &&
        (!options.CaloriesTo.HasValue || product.CaloriesPerBase <= options.CaloriesTo.Value) &&
        (!options.HasImage.HasValue || HasImage(product) == options.HasImage.Value);

    private static bool HasImage(ProductOverviewReadItem product) =>
        product.ImageUrl is not null || product.ImageAssetId is not null;

    private static PagedResponse<ProductModel> CreatePagedProducts(
        IReadOnlyList<ProductOverviewReadItem> products,
        IReadOnlyDictionary<ProductId, FavoriteProduct> favoritesByProductId,
        ProductOverviewOptions options,
        int totalItems) =>
        new(
            ToProductModels(products, favoritesByProductId).ToList(),
            options.PageNumber,
            options.PageSize,
            (int)Math.Ceiling(totalItems / (double)options.PageSize),
            totalItems);

    private static ProductModel[] ToProductModels(
        IEnumerable<ProductOverviewReadItem> products,
        IReadOnlyDictionary<ProductId, FavoriteProduct> favoritesByProductId) =>
        [.. products.Select(product => ToProductModel(product, favoritesByProductId))];

    private static ProductModel ToProductModel(
        ProductOverviewReadItem product,
        IReadOnlyDictionary<ProductId, FavoriteProduct> favoritesByProductId) {
        FavoriteProduct? favorite = favoritesByProductId.GetValueOrDefault(product.Id);
        return product.ToModel(favorite is not null, favorite?.Id.Value);
    }
}
