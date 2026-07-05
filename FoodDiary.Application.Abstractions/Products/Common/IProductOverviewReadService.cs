using FoodDiary.Application.Abstractions.Products.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Products.Common;

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
