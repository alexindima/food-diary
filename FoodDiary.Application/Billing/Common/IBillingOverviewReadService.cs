using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Billing.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Billing.Common;

public interface IBillingOverviewReadService {
    Task<Result<BillingOverviewModel>> GetAsync(UserId userId, CancellationToken cancellationToken);
}
