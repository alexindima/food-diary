using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Billing.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Billing.Services;

internal sealed class BillingUserLookupService(IUserDirectoryService userRepository) : IBillingUserLookupService {
    public Task<User?> GetUserIncludingDeletedAsync(UserId userId, CancellationToken cancellationToken) =>
        userRepository.GetByIdIncludingDeletedAsync(userId, cancellationToken);

    public Task<bool> CanAccessUserAsync(User user, CancellationToken cancellationToken) =>
        Task.FromResult(user.DeletedAt is null && user.IsActive);
}
