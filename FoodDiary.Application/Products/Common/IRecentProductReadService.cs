using FoodDiary.Application.Abstractions.Products.Models;
using FoodDiary.Application.Products.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Products.Common;

public interface IRecentProductReadService {
    Task<IReadOnlyList<ProductModel>> GetRecentAsync(
        UserId userId,
        int limit,
        bool includePublic,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductOverviewReadItem>> GetRecentOverviewItemsAsync(
        UserId userId,
        int limit,
        bool includePublic,
        ProductQueryFilters filters,
        CancellationToken cancellationToken = default);
}
