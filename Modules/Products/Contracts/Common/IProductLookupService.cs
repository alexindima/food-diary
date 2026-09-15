using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Products.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Products.Contracts.Common;

public interface IProductLookupService {
    Task<IReadOnlyDictionary<ProductId, ProductOverviewReadItem>> GetAccessibleByIdsAsync(
        IEnumerable<ProductId> ids,
        UserId userId,
        CancellationToken cancellationToken = default);
}
