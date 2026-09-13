using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Products.Common;

public interface IProductUsageQuery {
    Task<int> GetUsageCountAsync(ProductId id, UserId userId, bool includePublic = true, CancellationToken cancellationToken = default);
}
