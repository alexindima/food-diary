using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Billing.Common;
using FoodDiary.Application.Abstractions.Billing.Models;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Results;
using FoodDiary.Application.Billing.Common;
using FoodDiary.Application.Billing.Models;
using FoodDiary.Mediator;
using FoodDiary.Domain.Entities.Billing;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Billing.Commands.StartPremiumTrial;

public sealed class StartPremiumTrialCommandHandler(
    IBillingUserContextService billingUserContextService,
    IBillingSubscriptionReadRepository billingSubscriptionRepository,
    IBillingPublicConfigProvider billingPublicConfigProvider,
    TimeProvider dateTimeProvider)
    : IRequestHandler<StartPremiumTrialCommand, Result<BillingOverviewModel>> {
    private static readonly TimeSpan TrialDuration = TimeSpan.FromDays(7);

    public async Task<Result<BillingOverviewModel>> Handle(
        StartPremiumTrialCommand request,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await BillingCurrentUserAccessResolver.ResolveAsync(
            request.UserId,
            billingUserContextService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return BillingCurrentUserAccessResolver.ToFailure<BillingOverviewModel>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        Result<UserBillingProfileModel> userResult = await billingUserContextService.GetAccessibleUserAsync(userId, cancellationToken).ConfigureAwait(false);
        if (userResult.IsFailure) {
            return Result.Failure<BillingOverviewModel>(userResult.Error);
        }

        UserBillingProfileModel user = userResult.Value;
        BillingSubscription? subscription = await billingSubscriptionRepository.GetByUserIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (user.HasPaidPremium || IsPaidPremiumActive(subscription)) {
            return Result.Failure<BillingOverviewModel>(Errors.Billing.SubscriptionAlreadyActive);
        }

        if (user.PremiumTrialStartedAtUtc is not null || user.PremiumTrialEndsAtUtc is not null) {
            return Result.Failure<BillingOverviewModel>(Errors.Billing.TrialAlreadyUsed);
        }

        DateTime nowUtc = dateTimeProvider.GetUtcNow().UtcDateTime;
        Result<UserBillingProfileModel> startedTrialResult = await billingUserContextService
            .StartPremiumTrialAsync(userId, nowUtc, TrialDuration, cancellationToken)
            .ConfigureAwait(false);
        if (startedTrialResult.IsFailure) {
            return Result.Failure<BillingOverviewModel>(startedTrialResult.Error);
        }

        UserBillingProfileModel updatedUser = startedTrialResult.Value;

        BillingPublicConfigModel publicConfig = billingPublicConfigProvider.GetPublicConfig();
        return Result.Success(new BillingOverviewModel(
            IsPremium: true,
            "trialing",
            Plan: null,
            SubscriptionProvider: null,
            updatedUser.PremiumTrialStartedAtUtc,
            updatedUser.PremiumTrialEndsAtUtc,
            NextBillingAttemptUtc: null,
            CancelAtPeriodEnd: false,
            RenewalEnabled: false,
            ManageBillingAvailable: false,
            updatedUser.PremiumTrialStartedAtUtc,
            updatedUser.PremiumTrialEndsAtUtc,
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
