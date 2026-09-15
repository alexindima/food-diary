using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Products.Application.Abstractions.Common;

public interface IProductUsageQuery {
    Task<int> GetUsageCountAsync(ProductId id, UserId userId, bool includePublic = true, CancellationToken cancellationToken = default);
}
