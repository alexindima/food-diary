using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Abstractions.Products.Common;
using FoodDiary.Application.Abstractions.Products.Models;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.FavoriteProducts.Common;
using FoodDiary.Application.FavoriteProducts.Models;
using FoodDiary.Application.Products.Mappings;
using FoodDiary.Application.Products.Models;
using FoodDiary.Application.Products.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Products.Queries.GetProductsOverview;

public sealed class GetProductsOverviewQueryHandler(
    IProductOverviewReadService productOverviewReadService,
    IRecentProductReadService recentProductReadService,
    IFavoriteProductReadService favoriteProductReadService,
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
        IReadOnlyList<FavoriteProductModel> allFavorites = await favoriteProductReadService.GetAllAsync(options.UserId, cancellationToken).ConfigureAwait(false);
        var favoriteItems = allFavorites
            .Take(options.FavoriteLimit)
            .ToList();
        var favoriteLookup = allFavorites.ToDictionary(favorite => new ProductId(favorite.ProductId));

        IReadOnlyList<ProductOverviewReadItem> recentItems = await recentProductReadService.GetRecentOverviewItemsAsync(
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

    private static ProductOverviewOptions CreateOptions(GetProductsOverviewQuery query, UserId userId) {
        ProductType[]? productTypes = EnumFilterParser.ParseMany<ProductType>(query.ProductTypes);

        return new ProductOverviewOptions(
            userId,
            Math.Max(query.Page, 1),
            Math.Max(query.Limit, 1),
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
}
