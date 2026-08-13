using FoodDiary.Application.Abstractions.Products.Common;
using FoodDiary.Application.Abstractions.Products.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Infrastructure.Services;

public sealed class ProductLookupService(IProductOverviewReadService productOverviewReadService) : IProductLookupService {
    public Task<IReadOnlyDictionary<ProductId, ProductOverviewReadItem>> GetAccessibleByIdsAsync(
        IEnumerable<ProductId> ids,
        UserId userId,
        CancellationToken cancellationToken = default) =>
        productOverviewReadService.GetByIdsWithUsageAsync(ids, userId, includePublic: true, cancellationToken);
}
