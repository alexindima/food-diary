using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Products.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Products.Contracts.Common;

public interface IProductOverviewReadService {
    Task<(IReadOnlyList<ProductOverviewReadItem> Items, int TotalItems)> GetPagedAsync(
        UserId userId,
        bool includePublic,
        int page,
        int limit,
        ProductQueryFilters filters,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<ProductId, ProductOverviewReadItem>> GetByIdsWithUsageAsync(
        IEnumerable<ProductId> ids,
        UserId userId,
        bool includePublic = true,
        CancellationToken cancellationToken = default);
}
