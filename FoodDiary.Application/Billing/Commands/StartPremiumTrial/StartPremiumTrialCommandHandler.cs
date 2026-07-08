using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Billing.Common;
using FoodDiary.Application.Abstractions.Billing.Models;
using FoodDiary.Results;
using FoodDiary.Application.Billing.Common;
using FoodDiary.Application.Billing.Models;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Domain.Entities.Billing;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Billing.Commands.StartPremiumTrial;

public sealed class StartPremiumTrialCommandHandler(
    IBillingUserContextService billingUserContextService,
    IBillingSubscriptionReadRepository billingSubscriptionRepository,
    IBillingPublicConfigProvider billingPublicConfigProvider,
    TimeProvider dateTimeProvider)
    : ICommandHandler<StartPremiumTrialCommand, Result<BillingOverviewModel>> {
    private static readonly TimeSpan TrialDuration = TimeSpan.FromDays(7);

    public async Task<Result<BillingOverviewModel>> Handle(
        StartPremiumTrialCommand command,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(command.UserId);
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure<BillingOverviewModel>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        Result<User> userResult = await billingUserContextService.GetAccessibleUserAsync(userId, cancellationToken).ConfigureAwait(false);
        if (userResult.IsFailure) {
            return Result.Failure<BillingOverviewModel>(userResult.Error);
        }

        User user = userResult.Value;
        BillingSubscription? subscription = await billingSubscriptionRepository.GetByUserIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (user.HasRole(RoleNames.Premium) || IsPaidPremiumActive(subscription)) {
            return Result.Failure<BillingOverviewModel>(Errors.Billing.SubscriptionAlreadyActive);
        }

        if (user.HasUsedPremiumTrial()) {
            return Result.Failure<BillingOverviewModel>(Errors.Billing.TrialAlreadyUsed);
        }

        DateTime nowUtc = dateTimeProvider.GetUtcNow().UtcDateTime;
        user.StartPremiumTrial(nowUtc, TrialDuration);
        await billingUserContextService.UpdateUserAsync(user, cancellationToken).ConfigureAwait(false);

        BillingPublicConfigModel publicConfig = billingPublicConfigProvider.GetPublicConfig();
        return Result.Success(new BillingOverviewModel(
            IsPremium: true,
            "trialing",
            Plan: null,
            SubscriptionProvider: null,
            user.PremiumTrialStartedAtUtc,
            user.PremiumTrialEndsAtUtc,
            NextBillingAttemptUtc: null,
            CancelAtPeriodEnd: false,
            RenewalEnabled: false,
            ManageBillingAvailable: false,
            user.PremiumTrialStartedAtUtc,
            user.PremiumTrialEndsAtUtc,
            PremiumTrialActive: true,
            PremiumTrialUsed: true,
            CanStartPremiumTrial: false,
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
                subscription.CurrentPeriodEndUtc > dateTimeProvider.GetUtcNow().UtcDateTime,
            "active" => true,
            "past_due" => !subscription.CurrentPeriodEndUtc.HasValue ||
                subscription.CurrentPeriodEndUtc > dateTimeProvider.GetUtcNow().UtcDateTime,
            _ => false,
        };
    }
}
