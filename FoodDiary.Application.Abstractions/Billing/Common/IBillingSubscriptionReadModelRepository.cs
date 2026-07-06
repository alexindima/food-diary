using FoodDiary.Application.Abstractions.Billing.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Billing.Common;

public interface IBillingSubscriptionReadModelRepository {
    Task<BillingSubscriptionOverviewReadModel?> GetOverviewReadModelByUserIdAsync(
        UserId userId,
        CancellationToken cancellationToken = default);
}