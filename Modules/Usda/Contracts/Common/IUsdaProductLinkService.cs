using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Abstractions.Usda.Common;

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
