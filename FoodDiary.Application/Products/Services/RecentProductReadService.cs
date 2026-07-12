using FoodDiary.Application.Abstractions.Products.Common;
using FoodDiary.Application.Abstractions.Products.Models;
using FoodDiary.Application.Abstractions.RecentItems.Common;
using FoodDiary.Application.Products.Common;
using FoodDiary.Application.Products.Mappings;
using FoodDiary.Application.Products.Models;
using FoodDiary.Application.RecentItems.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Products.Services;

public sealed class RecentProductReadService(
    IRecentItemUsageReadService recentItemUsageReadService,
    IProductOverviewReadService productOverviewReadService)
    : IRecentProductReadService {
    public async Task<IReadOnlyList<ProductModel>> GetRecentAsync(
        UserId userId,
        int limit,
        bool includePublic,
        CancellationToken cancellationToken = default) {
        IReadOnlyList<ProductOverviewReadItem> items = await GetRecentItemsAsync(
            userId,
            limit,
            includePublic,
            cancellationToken).ConfigureAwait(false);

        return [.. items.Select(item => item.ToModel())];
    }

    public async Task<IReadOnlyList<ProductOverviewReadItem>> GetRecentOverviewItemsAsync(
        UserId userId,
        int limit,
        bool includePublic,
        ProductQueryFilters filters,
        CancellationToken cancellationToken = default) {
        if (!string.IsNullOrWhiteSpace(filters.Search)) {
            return [];
        }

        IReadOnlyList<ProductOverviewReadItem> items = await GetRecentItemsAsync(
            userId,
            limit,
            includePublic,
            cancellationToken).ConfigureAwait(false);

        return [.. items.Where(item => MatchesFilters(item, filters))];
    }

    private async Task<IReadOnlyList<ProductOverviewReadItem>> GetRecentItemsAsync(
        UserId userId,
        int limit,
        bool includePublic,
        CancellationToken cancellationToken) {
        return await RecentItemOverviewLoader.LoadAsync<RecentProductUsage, ProductId, ProductOverviewReadItem>(
            userId,
            limit,
            recentItemUsageReadService.GetRecentProductsAsync,
            recent => recent.ProductId,
            (ids, ownerUserId, ct) => productOverviewReadService.GetByIdsWithUsageAsync(ids, ownerUserId, includePublic, ct),
            cancellationToken).ConfigureAwait(false);
    }

    private static bool MatchesFilters(ProductOverviewReadItem product, ProductQueryFilters filters) =>
        (filters.ProductTypes?.Contains(product.ProductType) != false) &&
        (!filters.CaloriesFrom.HasValue || product.CaloriesPerBase >= filters.CaloriesFrom.Value) &&
        (!filters.CaloriesTo.HasValue || product.CaloriesPerBase <= filters.CaloriesTo.Value) &&
        (!filters.HasImage.HasValue || HasImage(product) == filters.HasImage.Value);

    private static bool HasImage(ProductOverviewReadItem product) =>
        product.ImageUrl is not null || product.ImageAssetId is not null;
}
