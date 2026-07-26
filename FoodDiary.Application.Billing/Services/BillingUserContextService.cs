using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Billing.Common;
using FoodDiary.Application.Billing.Models;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Billing.Services;

internal sealed class BillingUserContextService(
    IBillingUserLookupService userLookupService,
    IUserDirectoryService userDirectoryService,
    IUserWriteRepository userWriteRepository,
    IUserRoleMembershipService roleMembershipService) : IBillingUserContextService {
    public async Task<Result<BillingUserProfileModel>> GetAccessibleUserProfileAsync(
        UserId userId,
        CancellationToken cancellationToken) {
        User? user = await userDirectoryService.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        Error? accessError = GetAccessError(user);
        if (accessError is not null) {
            return Result.Failure<BillingUserProfileModel>(accessError);
        }

        User accessibleUser = user!;
        return Result.Success(new BillingUserProfileModel(
            accessibleUser.HasRole(RoleNames.Premium),
            accessibleUser.PremiumTrialStartedAtUtc,
            accessibleUser.PremiumTrialEndsAtUtc));
    }

    public async Task<Result<User>> GetAccessibleUserAsync(UserId userId, CancellationToken cancellationToken) {
        User? user = await userDirectoryService.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        Error? accessError = GetAccessError(user);
        return accessError is not null
            ? Result.Failure<User>(accessError)
            : Result.Success(user!);
    }

    public async Task<Error?> EnsureCanAccessAsync(UserId userId, CancellationToken cancellationToken = default) {
        User? user = await userDirectoryService.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        return GetAccessError(user);
    }

    public Task<User?> GetUserIncludingDeletedAsync(UserId userId, CancellationToken cancellationToken) =>
        userLookupService.GetUserIncludingDeletedAsync(userId, cancellationToken);

    public Task<bool> CanAccessUserAsync(User user, CancellationToken cancellationToken) =>
        userLookupService.CanAccessUserAsync(user, cancellationToken);

    public Task EnsurePremiumRoleAsync(User user, CancellationToken cancellationToken) =>
        roleMembershipService.EnsureRoleAsync(user.Id, RoleNames.Premium, cancellationToken);

    public Task RemovePremiumRoleAsync(User user, CancellationToken cancellationToken) =>
        roleMembershipService.RemoveRoleAsync(user.Id, RoleNames.Premium, cancellationToken);

    public Task UpdateUserAsync(User user, CancellationToken cancellationToken) =>
        userWriteRepository.UpdateAsync(user, cancellationToken);

    private static Error? GetAccessError(User? user) {
        if (user?.IsActive != true) {
            return Errors.Authentication.InvalidToken;
        }

        return user.DeletedAt is null ? null : Errors.Authentication.AccountDeleted;
    }
}
