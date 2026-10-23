using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Products.Application.Abstractions.Common;

/// <summary>Returns owned products still referenced by consumers surviving account purge.</summary>
public interface IProductUserPurgeRetentionReadService {
    Task<IReadOnlyCollection<ProductId>> GetRetainedProductIdsAsync(UserId sourceUserId, CancellationToken cancellationToken);
}
