using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Billing.Common;
using FoodDiary.Application.Billing.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Billing.Services;

internal sealed class BillingUserContextService(
    IBillingUserLookupService userLookupService,
    IUserContextService userContextService,
    IUserRoleMembershipService roleMembershipService) : IBillingUserContextService {
    public async Task<Result<BillingUserProfileModel>> GetAccessibleUserProfileAsync(
        UserId userId,
        CancellationToken cancellationToken) {
        Result<User> userResult = await userContextService.GetAccessibleUserAsync(userId, cancellationToken).ConfigureAwait(false);
        if (userResult.IsFailure) {
            return Result.Failure<BillingUserProfileModel>(userResult.Error);
        }

        User user = userResult.Value;
        return Result.Success(new BillingUserProfileModel(
            user.HasRole(RoleNames.Premium),
            user.PremiumTrialStartedAtUtc,
            user.PremiumTrialEndsAtUtc));
    }

    public Task<Result<User>> GetAccessibleUserAsync(UserId userId, CancellationToken cancellationToken) =>
        userContextService.GetAccessibleUserAsync(userId, cancellationToken);

    public Task<User?> GetUserIncludingDeletedAsync(UserId userId, CancellationToken cancellationToken) =>
        userLookupService.GetUserIncludingDeletedAsync(userId, cancellationToken);

    public Task<bool> CanAccessUserAsync(User user, CancellationToken cancellationToken) =>
        userLookupService.CanAccessUserAsync(user, cancellationToken);

    public Task EnsurePremiumRoleAsync(User user, CancellationToken cancellationToken) =>
        roleMembershipService.EnsureRoleAsync(user.Id, RoleNames.Premium, cancellationToken);

    public Task RemovePremiumRoleAsync(User user, CancellationToken cancellationToken) =>
        roleMembershipService.RemoveRoleAsync(user.Id, RoleNames.Premium, cancellationToken);

    public Task UpdateUserAsync(User user, CancellationToken cancellationToken) =>
        userContextService.UpdateUserAsync(user, cancellationToken);
}
