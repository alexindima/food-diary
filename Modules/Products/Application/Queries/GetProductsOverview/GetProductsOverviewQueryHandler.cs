using FoodDiary.Modules.Products.Application.Mappings;
using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Products.Contracts.Common;
using FoodDiary.Modules.Products.Contracts.Models;
using FoodDiary.Modules.Products.Application.Common;
using FoodDiary.Modules.Products.Application.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Products.Application.Services;
using FoodDiary.Modules.Products.Domain.Contracts.Enums;
using FoodDiary.Mediator;
using FoodDiary.Modules.Favorites.Contracts.FavoriteProducts.Queries.ReadFavoriteProducts;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Application.Abstractions.Common.Models;
using FoodDiary.Modules.Favorites.Contracts.FavoriteProducts.Models;

namespace FoodDiary.Modules.Products.Application.Queries.GetProductsOverview;

public sealed class GetProductsOverviewQueryHandler(
    IProductOverviewReadService productOverviewReadService,
    RecentProductLoader recentProductLoader,
    ISender sender,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetProductsOverviewQuery, Result<ProductOverviewModel>> {
    private sealed record ProductOverviewOptions(
        UserId UserId,
        int PageNumber,
        int PageSize,
        int RecentLimit,
        int FavoriteLimit,
        ProductType[]? ProductTypes,
        double? CaloriesFrom,
        double? CaloriesTo,
        bool? HasImage) {
        public ProductQueryFilters ToFilters(string? search) =>
            new(search, ProductTypes, CaloriesFrom, CaloriesTo, HasImage);
    }

    public async Task<Result<ProductOverviewModel>> Handle(
        GetProductsOverviewQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<ProductOverviewModel>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        ProductOverviewOptions options = CreateOptions(query, userId);

        (IReadOnlyList<ProductOverviewReadItem> items, int totalItems) = await productOverviewReadService.GetPagedAsync(
            options.UserId,
            query.IncludePublic,
            options.PageNumber,
            options.PageSize,
            options.ToFilters(query.Search),
            cancellationToken).ConfigureAwait(false);

        var allProducts = items.ToList();
        IReadOnlyList<FavoriteProductModel> allFavorites = await sender.Send(new ReadFavoriteProductsQuery(options.UserId), cancellationToken).ConfigureAwait(false);
        var favoriteItems = allFavorites
            .Take(options.FavoriteLimit)
            .ToList();
        var favoriteLookup = allFavorites.ToDictionary(ToFavoriteProductId);

        IReadOnlyList<ProductOverviewReadItem> recentItems = await GetRecentOverviewItemsAsync(
            options.UserId,
            options.RecentLimit,
            query.IncludePublic,
            options.ToFilters(query.Search),
            cancellationToken).ConfigureAwait(false);
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

    private static ProductId ToFavoriteProductId(FavoriteProductModel favorite) =>
        new(favorite.ProductId);

    private static ProductOverviewOptions CreateOptions(GetProductsOverviewQuery query, UserId userId) {
        ProductType[]? productTypes = ProductEnumFilterParser.ParseMany<ProductType>(query.ProductTypes);

        return new ProductOverviewOptions(
            userId,
            PaginationPolicy.NormalizePage(query.Page),
            PaginationPolicy.NormalizePageSize(query.Limit, defaultPageSize: 1),
            Math.Clamp(query.RecentLimit, 1, 50),
            Math.Clamp(query.FavoriteLimit, 1, 50),
            productTypes is { Length: > 0 } ? productTypes : null,
            query.CaloriesFrom,
            query.CaloriesTo,
            query.HasImage);
    }

    private static PagedResponse<ProductModel> CreatePagedProducts(
        IReadOnlyList<ProductOverviewReadItem> products,
        IReadOnlyDictionary<ProductId, FavoriteProductModel> favoritesByProductId,
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
        IReadOnlyDictionary<ProductId, FavoriteProductModel> favoritesByProductId) =>
        [.. products.Select(product => ToProductModel(product, favoritesByProductId))];

    private static ProductModel ToProductModel(
        ProductOverviewReadItem product,
        IReadOnlyDictionary<ProductId, FavoriteProductModel> favoritesByProductId) {
        FavoriteProductModel? favorite = favoritesByProductId.GetValueOrDefault(product.Id);
        return product.ToModel(favorite is not null, favorite?.Id);
    }
    private async Task<IReadOnlyList<ProductOverviewReadItem>> GetRecentOverviewItemsAsync(
        UserId userId,
        int limit,
        bool includePublic,
        ProductQueryFilters filters,
        CancellationToken cancellationToken = default) {
        if (!string.IsNullOrWhiteSpace(filters.Search)) {
            return [];
        }

        IReadOnlyList<ProductOverviewReadItem> items = await recentProductLoader.LoadAsync(
            userId,
            limit,
            includePublic,
            cancellationToken).ConfigureAwait(false);

        return [.. items.Where(item => MatchesFilters(item, filters))];
    }
    private static bool MatchesFilters(ProductOverviewReadItem product, ProductQueryFilters filters) =>
        (filters.ProductTypes?.Contains(product.ProductType) != false) &&
        (!filters.CaloriesFrom.HasValue || product.CaloriesPerBase >= filters.CaloriesFrom.Value) &&
        (!filters.CaloriesTo.HasValue || product.CaloriesPerBase <= filters.CaloriesTo.Value) &&
        (!filters.HasImage.HasValue || HasImage(product) == filters.HasImage.Value);

    private static bool HasImage(ProductOverviewReadItem product) =>
        product.ImageUrl is not null || product.ImageAssetId is not null;

}
