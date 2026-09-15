using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Products.Contracts.Common;
using FoodDiary.Modules.Products.Contracts.Models;
using FoodDiary.Modules.RecentItems.Contracts.Common;
using FoodDiary.Modules.Products.Application.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Mediator;
using FoodDiary.Modules.RecentItems.Contracts.Queries.ReadRecentProducts;

namespace FoodDiary.Modules.Products.Application.Services;

public sealed class RecentProductLoader(ISender sender, IProductOverviewReadService productOverviewReadService) {
    public async Task<IReadOnlyList<ProductOverviewReadItem>> LoadAsync(
        UserId userId,
        int limit,
        bool includePublic,
        CancellationToken cancellationToken) {
        return await RecentProductOverviewLoader.LoadAsync<RecentProductUsage, ProductId, ProductOverviewReadItem>(
            userId,
            limit,
            (owner, count, token) => sender.Send(new ReadRecentProductsQuery(owner, count), token),
            recent => recent.ProductId,
            (ids, ownerUserId, ct) => productOverviewReadService.GetByIdsWithUsageAsync(ids, ownerUserId, includePublic, ct),
            cancellationToken).ConfigureAwait(false);
    }
}
