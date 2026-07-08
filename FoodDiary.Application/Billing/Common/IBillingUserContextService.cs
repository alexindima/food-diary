using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Billing.Models;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Billing.Common;

public interface IBillingUserContextService : ICurrentUserAccessService {
    Task<Result<BillingUserProfileModel>> GetAccessibleUserProfileAsync(UserId userId, CancellationToken cancellationToken);
    Task<Result<User>> GetAccessibleUserAsync(UserId userId, CancellationToken cancellationToken);
    Task<User?> GetUserIncludingDeletedAsync(UserId userId, CancellationToken cancellationToken);
    Task<bool> CanAccessUserAsync(User user, CancellationToken cancellationToken);
    Task EnsurePremiumRoleAsync(User user, CancellationToken cancellationToken);
    Task RemovePremiumRoleAsync(User user, CancellationToken cancellationToken);
    Task UpdateUserAsync(User user, CancellationToken cancellationToken);
}
