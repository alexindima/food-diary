using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Billing.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Billing.Services;

internal sealed class BillingUserContextService(
    IUserRepository userRepository,
    IUserContextService userContextService,
    IUserRoleMembershipService roleMembershipService) : IBillingUserContextService {
    public Task<Result<User>> GetAccessibleUserAsync(UserId userId, CancellationToken cancellationToken) =>
        userContextService.GetAccessibleUserAsync(userId, cancellationToken);

    public Task<User?> GetUserIncludingDeletedAsync(UserId userId, CancellationToken cancellationToken) =>
        userRepository.GetByIdIncludingDeletedAsync(userId, cancellationToken);

    public Task<bool> CanAccessUserAsync(User user, CancellationToken cancellationToken) =>
        Task.FromResult(CurrentUserAccessPolicy.EnsureCanAccess(user) is null);

    public Task EnsurePremiumRoleAsync(User user, CancellationToken cancellationToken) =>
        roleMembershipService.EnsureRoleAsync(user.Id, RoleNames.Premium, cancellationToken);

    public Task RemovePremiumRoleAsync(User user, CancellationToken cancellationToken) =>
        roleMembershipService.RemoveRoleAsync(user.Id, RoleNames.Premium, cancellationToken);

    public Task UpdateUserAsync(User user, CancellationToken cancellationToken) =>
        userContextService.UpdateUserAsync(user, cancellationToken);
}
