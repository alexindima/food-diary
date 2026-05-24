using FoodDiary.Application.Abstractions.Billing.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Common.Interfaces.Services;
using FoodDiary.Application.Billing.Models;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.Billing;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Billing.Commands.StartPremiumTrial;

public sealed class StartPremiumTrialCommandHandler(
    IUserRepository userRepository,
    IBillingSubscriptionRepository billingSubscriptionRepository,
    IBillingPublicConfigProvider billingPublicConfigProvider,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<StartPremiumTrialCommand, Result<BillingOverviewModel>> {
    private static readonly TimeSpan TrialDuration = TimeSpan.FromDays(7);

    public async Task<Result<BillingOverviewModel>> Handle(
        StartPremiumTrialCommand command,
        CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<BillingOverviewModel>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId.Value);
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        var accessError = CurrentUserAccessPolicy.EnsureCanAccess(user);
        if (accessError is not null) {
            return Result.Failure<BillingOverviewModel>(accessError);
        }

        var subscription = await billingSubscriptionRepository.GetByUserIdAsync(userId, cancellationToken);
        if (user!.HasRole(RoleNames.Premium) || IsPaidPremiumActive(subscription)) {
            return Result.Failure<BillingOverviewModel>(Errors.Billing.SubscriptionAlreadyActive);
        }

        if (user.HasUsedPremiumTrial()) {
            return Result.Failure<BillingOverviewModel>(Errors.Billing.TrialAlreadyUsed);
        }

        var nowUtc = dateTimeProvider.UtcNow;
        user.StartPremiumTrial(nowUtc, TrialDuration);
        await userRepository.UpdateAsync(user, cancellationToken);

        var publicConfig = billingPublicConfigProvider.GetPublicConfig();
        return Result.Success(new BillingOverviewModel(
            true,
            "trialing",
            null,
            null,
            user.PremiumTrialStartedAtUtc,
            user.PremiumTrialEndsAtUtc,
            null,
            false,
            false,
            false,
            user.PremiumTrialStartedAtUtc,
            user.PremiumTrialEndsAtUtc,
            true,
            true,
            false,
            publicConfig.Provider,
            publicConfig.PaddleClientToken,
            publicConfig.AvailableProviders));
    }

    private bool IsPaidPremiumActive(BillingSubscription? subscription) {
        if (subscription is null) {
            return false;
        }

        if (string.IsNullOrWhiteSpace(subscription.Status)) {
            return false;
        }

        return subscription.Status.Trim().ToLowerInvariant() switch {
            "trialing" => subscription.CurrentPeriodEndUtc.HasValue &&
                subscription.CurrentPeriodEndUtc > dateTimeProvider.UtcNow,
            "active" => true,
            "past_due" => !subscription.CurrentPeriodEndUtc.HasValue ||
                subscription.CurrentPeriodEndUtc > dateTimeProvider.UtcNow,
            _ => false
        };
    }
}
