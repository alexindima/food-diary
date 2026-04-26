using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Products.Common;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Infrastructure.Services;

public class ProductLookupService(IProductRepository productRepository) : IProductLookupService {
    public Task<IReadOnlyDictionary<ProductId, Product>> GetAccessibleByIdsAsync(
        IEnumerable<ProductId> ids,
        UserId userId,
        CancellationToken cancellationToken = default) =>
        productRepository.GetByIdsAsync(ids, userId, includePublic: true, cancellationToken);
}
