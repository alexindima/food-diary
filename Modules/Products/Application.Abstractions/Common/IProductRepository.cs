using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Products.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Products.Application.Abstractions.Common;

public interface IProductRepository : IProductReadRepository, IProductWriteRepository {
    Task<Product?> IProductWriteRepository.GetByIdForUpdateAsync(
        ProductId id,
        UserId userId,
        bool includePublic,
        CancellationToken cancellationToken) =>
        GetByIdAsync(id, userId, includePublic, cancellationToken);
}
