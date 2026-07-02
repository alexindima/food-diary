using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Billing.Common;

public interface IBillingUserContextService {
    Task<Result<User>> GetAccessibleUserAsync(UserId userId, CancellationToken cancellationToken);
}
