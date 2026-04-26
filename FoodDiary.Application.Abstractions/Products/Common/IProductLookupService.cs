using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Products.Common;

public interface IProductLookupService {
    Task<IReadOnlyDictionary<ProductId, Product>> GetAccessibleByIdsAsync(
        IEnumerable<ProductId> ids,
        UserId userId,
        CancellationToken cancellationToken = default);
}
