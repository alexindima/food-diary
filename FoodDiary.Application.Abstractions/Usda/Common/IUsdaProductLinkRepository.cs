using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Usda.Common;

public interface IUsdaProductLinkRepository {
    Task<Product?> GetForLinkUpdateAsync(
        ProductId productId,
        UserId userId,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(Product product, CancellationToken cancellationToken = default);
}
