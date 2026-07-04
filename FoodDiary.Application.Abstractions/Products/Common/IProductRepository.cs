using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Products.Common;

public interface IProductRepository : IProductReadRepository, IProductWriteRepository {
    Task<Product?> IProductWriteRepository.GetByIdForUpdateAsync(
        ProductId id,
        UserId userId,
        bool includePublic,
        CancellationToken cancellationToken) =>
        GetByIdAsync(id, userId, includePublic, cancellationToken);
}
