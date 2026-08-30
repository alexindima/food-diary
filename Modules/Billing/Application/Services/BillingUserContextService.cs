using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Application.Billing.Common;
using FoodDiary.Application.Billing.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Billing.Services;

internal sealed class BillingUserContextService(
    IUserBillingService userBillingService) : IBillingUserContextService {
    public async Task<Result<BillingUserProfileModel>> GetAccessibleUserProfileAsync(
        UserId userId,
        CancellationToken cancellationToken) {
        Result<UserBillingProfileModel> userResult = await userBillingService
            .GetAccessibleProfileAsync(userId, cancellationToken)
            .ConfigureAwait(false);
        if (userResult.IsFailure) {
            return Result.Failure<BillingUserProfileModel>(userResult.Error);
        }

        UserBillingProfileModel accessibleUser = userResult.Value;
        return Result.Success(new BillingUserProfileModel(
            accessibleUser.HasPaidPremium,
            accessibleUser.PremiumTrialStartedAtUtc,
            accessibleUser.PremiumTrialEndsAtUtc));
    }

    public Task<Result<UserBillingProfileModel>> GetAccessibleUserAsync(UserId userId, CancellationToken cancellationToken) =>
        userBillingService.GetAccessibleProfileAsync(userId, cancellationToken);

    public Task<Error?> EnsureCanAccessAsync(UserId userId, CancellationToken cancellationToken = default) =>
        userBillingService.EnsureCanAccessAsync(userId, cancellationToken);

    public Task<UserBillingProfileModel?> GetUserIncludingDeletedAsync(UserId userId, CancellationToken cancellationToken) =>
        userBillingService.GetProfileIncludingDeletedAsync(userId, cancellationToken);

    public Task<Result<UserBillingProfileModel>> StartPremiumTrialAsync(
        UserId userId,
        DateTime startedAtUtc,
        TimeSpan duration,
        CancellationToken cancellationToken) =>
        userBillingService.StartPremiumTrialAsync(userId, startedAtUtc, duration, cancellationToken);

    public Task EnsurePremiumRoleAsync(UserId userId, CancellationToken cancellationToken) =>
        userBillingService.EnsurePremiumRoleAsync(userId, cancellationToken);

    public Task RemovePremiumRoleAsync(UserId userId, CancellationToken cancellationToken) =>
        userBillingService.RemovePremiumRoleAsync(userId, cancellationToken);
}
