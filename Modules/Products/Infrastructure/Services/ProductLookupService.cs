using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Products.Contracts.Common;
using FoodDiary.Modules.Products.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Products.Infrastructure.Services;

public sealed class ProductLookupService(IProductOverviewReadService productOverviewReadService) : IProductLookupService {
    public Task<IReadOnlyDictionary<ProductId, ProductOverviewReadItem>> GetAccessibleByIdsAsync(
        IEnumerable<ProductId> ids,
        UserId userId,
        CancellationToken cancellationToken = default) =>
        productOverviewReadService.GetByIdsWithUsageAsync(ids, userId, includePublic: true, cancellationToken);
}
