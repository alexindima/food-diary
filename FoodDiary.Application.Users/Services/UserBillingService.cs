using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Users.Services;

internal sealed class UserBillingService(
    IUserLookupRepository userLookupRepository,
    IUserWriteRepository userWriteRepository,
    IUserRoleMembershipService roleMembershipService) : IUserBillingService {
    public async Task<Result<UserBillingProfileModel>> GetAccessibleProfileAsync(
        UserId userId,
        CancellationToken cancellationToken = default) {
        User? user = await userLookupRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        Error? error = CurrentUserAccessPolicy.EnsureCanAccess(user);
        return error is null
            ? Result.Success(ToModel(user!))
            : Result.Failure<UserBillingProfileModel>(error);
    }

    public async Task<UserBillingProfileModel?> GetProfileIncludingDeletedAsync(
        UserId userId,
        CancellationToken cancellationToken = default) {
        User? user = await userLookupRepository
            .GetByIdIncludingDeletedAsync(userId, cancellationToken)
            .ConfigureAwait(false);
        return user is null ? null : ToModel(user);
    }

    public async Task<Result<UserBillingProfileModel>> StartPremiumTrialAsync(
        UserId userId,
        DateTime startedAtUtc,
        TimeSpan duration,
        CancellationToken cancellationToken = default) {
        User? user = await userLookupRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        Error? error = CurrentUserAccessPolicy.EnsureCanAccess(user);
        if (error is not null) {
            return Result.Failure<UserBillingProfileModel>(error);
        }

        user!.StartPremiumTrial(startedAtUtc, duration);
        await userWriteRepository.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
        return Result.Success(ToModel(user));
    }

    public Task EnsurePremiumRoleAsync(UserId userId, CancellationToken cancellationToken = default) =>
        roleMembershipService.EnsureRoleAsync(userId, RoleNames.Premium, cancellationToken);

    public Task RemovePremiumRoleAsync(UserId userId, CancellationToken cancellationToken = default) =>
        roleMembershipService.RemoveRoleAsync(userId, RoleNames.Premium, cancellationToken);

    public async Task<Error?> EnsureCanAccessAsync(UserId userId, CancellationToken cancellationToken = default) {
        User? user = await userLookupRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        return CurrentUserAccessPolicy.EnsureCanAccess(user);
    }

    private static UserBillingProfileModel ToModel(User user) =>
        new(
            user.Id,
            user.Email,
            user.IsActive,
            user.DeletedAt is not null,
            user.HasRole(RoleNames.Premium),
            user.PremiumTrialStartedAtUtc,
            user.PremiumTrialEndsAtUtc);
}
