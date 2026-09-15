using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Usda.Contracts.Common;

public interface IUsdaProductLinkService {
    Task<Result> IsAccessibleForUpdateAsync(
        ProductId productId,
        UserId userId,
        CancellationToken cancellationToken = default);

    Task<Result> LinkAsync(
        ProductId productId,
        UserId userId,
        int fdcId,
        CancellationToken cancellationToken = default);

    Task<Result> UnlinkAsync(
        ProductId productId,
        UserId userId,
        CancellationToken cancellationToken = default);
}
