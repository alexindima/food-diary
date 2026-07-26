using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Billing.Common;

public interface IBillingUserLookupService {
    Task<User?> GetUserIncludingDeletedAsync(UserId userId, CancellationToken cancellationToken);
    Task<bool> CanAccessUserAsync(User user, CancellationToken cancellationToken);
}
