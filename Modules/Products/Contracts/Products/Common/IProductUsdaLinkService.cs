using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Products.Common;

public interface IProductUsdaLinkService {
    Task<bool> IsAccessibleForUpdateAsync(
        ProductId productId,
        UserId userId,
        CancellationToken cancellationToken = default);

    Task<bool> LinkAsync(
        ProductId productId,
        UserId userId,
        int fdcId,
        CancellationToken cancellationToken = default);

    Task<bool> UnlinkAsync(
        ProductId productId,
        UserId userId,
        CancellationToken cancellationToken = default);
}
