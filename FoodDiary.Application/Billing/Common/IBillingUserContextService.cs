using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Billing.Common;

public interface IBillingUserContextService {
    Task<Result<User>> GetAccessibleUserAsync(UserId userId, CancellationToken cancellationToken);
    Task<User?> GetUserIncludingDeletedAsync(UserId userId, CancellationToken cancellationToken);
    Task<bool> CanAccessUserAsync(User user, CancellationToken cancellationToken);
    Task EnsurePremiumRoleAsync(User user, CancellationToken cancellationToken);
    Task RemovePremiumRoleAsync(User user, CancellationToken cancellationToken);
    Task UpdateUserAsync(User user, CancellationToken cancellationToken);
}
