using FoodDiary.Modules.Billing.Application.Abstractions.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Billing.Application.Abstractions.Common;

public interface IBillingSubscriptionReadModelRepository {
    Task<BillingSubscriptionOverviewReadModel?> GetOverviewReadModelByUserIdAsync(
        UserId userId,
        CancellationToken cancellationToken = default);
}
