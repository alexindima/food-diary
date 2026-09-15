using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Products.Contracts.Common;
using FoodDiary.Modules.Products.Contracts.Models;
using FoodDiary.Modules.RecentItems.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Mediator;
using FoodDiary.Modules.RecentItems.Contracts.Queries.ReadRecentProducts;

namespace FoodDiary.Modules.Products.Application.Services;

public sealed class RecentProductLoader(ISender sender, IProductOverviewReadService productOverviewReadService) {
    public async Task<IReadOnlyList<ProductOverviewReadItem>> LoadAsync(
        UserId userId,
        int limit,
        bool includePublic,
        CancellationToken cancellationToken) {
        IReadOnlyList<RecentProductUsage> recents = await sender.Send(new ReadRecentProductsQuery(userId, limit), cancellationToken)
            .ConfigureAwait(false);
        if (recents.Count == 0) {
            return [];
        }

        ProductId[] idsInOrder = [.. recents.Select(recent => recent.ProductId)];
        IReadOnlyDictionary<ProductId, ProductOverviewReadItem> itemsById = await productOverviewReadService
            .GetByIdsWithUsageAsync(idsInOrder, userId, includePublic, cancellationToken)
            .ConfigureAwait(false);

        return [.. idsInOrder.Where(itemsById.ContainsKey).Select(id => itemsById[id])];
    }
}
