using FoodDiary.Application.Abstractions.Billing.Common;
using FoodDiary.Application.Billing.Models;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Common.Interfaces.Services;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.Billing;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Application.Abstractions.Billing.Models;

namespace FoodDiary.Application.Billing.Queries.GetBillingOverview;

public sealed class GetBillingOverviewQueryHandler(
    IUserRepository userRepository,
    IBillingSubscriptionRepository billingSubscriptionRepository,
    IBillingPublicConfigProvider billingPublicConfigProvider,
    IDateTimeProvider dateTimeProvider)
    : IQueryHandler<GetBillingOverviewQuery, Result<BillingOverviewModel>> {
    public async Task<Result<BillingOverviewModel>> Handle(
        GetBillingOverviewQuery query,
        CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<BillingOverviewModel>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(query.UserId.Value);
        User? user = await userRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        Error? accessError = CurrentUserAccessPolicy.EnsureCanAccess(user);
        if (accessError is not null) {
            return Result.Failure<BillingOverviewModel>(accessError);
        }

        BillingSubscription? subscription = await billingSubscriptionRepository.GetByUserIdAsync(userId, cancellationToken).ConfigureAwait(false);
        DateTime nowUtc = dateTimeProvider.UtcNow;
        bool isTrialActive = user!.HasActivePremiumTrial(nowUtc);
        bool hasPaidPremium = user.HasRole(RoleNames.Premium);
        bool paidSubscriptionActive = IsPaidPremiumActive(subscription, nowUtc);
        bool isPremium = hasPaidPremium || paidSubscriptionActive || isTrialActive;
        bool providerTrialExpired = IsExpiredProviderTrial(subscription, nowUtc);
        string? subscriptionStatus = providerTrialExpired
            ? (isTrialActive ? "trialing" : null)
            : subscription?.Status ?? (isTrialActive ? "trialing" : null);
        DateTime? currentPeriodStartUtc = providerTrialExpired
            ? (isTrialActive ? user.PremiumTrialStartedAtUtc : null)
            : subscription?.CurrentPeriodStartUtc ?? (isTrialActive ? user.PremiumTrialStartedAtUtc : null);
        DateTime? currentPeriodEndUtc = providerTrialExpired
            ? (isTrialActive ? user.PremiumTrialEndsAtUtc : null)
            : subscription?.CurrentPeriodEndUtc ?? (isTrialActive ? user.PremiumTrialEndsAtUtc : null);
        BillingPublicConfigModel publicConfig = billingPublicConfigProvider.GetPublicConfig();
        bool renewalEnabled = subscription?.NextBillingAttemptUtc is not null &&
            !subscription.CancelAtPeriodEnd;
        bool canStartTrial = !hasPaidPremium && !paidSubscriptionActive && !user.HasUsedPremiumTrial();

        return Result.Success(new BillingOverviewModel(
            isPremium,
            subscriptionStatus,
            subscription?.Plan,
            subscription?.Provider,
            currentPeriodStartUtc,
            currentPeriodEndUtc,
            subscription?.NextBillingAttemptUtc,
            subscription?.CancelAtPeriodEnd ?? false,
            renewalEnabled,
            subscription is not null &&
                !string.Equals(subscription.Provider, BillingProviderNames.YooKassa, StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(subscription.ExternalCustomerId),
            user.PremiumTrialStartedAtUtc,
            user.PremiumTrialEndsAtUtc,
            isTrialActive,
            user.HasUsedPremiumTrial(),
            canStartTrial,
            publicConfig.Provider,
            publicConfig.PaddleClientToken,
            publicConfig.AvailableProviders));
    }

    private static bool IsPaidPremiumActive(BillingSubscription? subscription, DateTime nowUtc) {
        if (subscription is null || string.IsNullOrWhiteSpace(subscription.Status)) {
            return false;
        }

        return subscription.Status.Trim().ToLowerInvariant() switch {
            "trialing" => subscription.CurrentPeriodEndUtc.HasValue && subscription.CurrentPeriodEndUtc > nowUtc,
            "active" => true,
            "past_due" => !subscription.CurrentPeriodEndUtc.HasValue || subscription.CurrentPeriodEndUtc > nowUtc,
            _ => false
        };
    }

    private static bool IsExpiredProviderTrial(BillingSubscription? subscription, DateTime nowUtc) {
        if (subscription is null ||
            !string.Equals(subscription.Status, "trialing", StringComparison.OrdinalIgnoreCase)) {
            return false;
        }

        return !subscription.CurrentPeriodEndUtc.HasValue || subscription.CurrentPeriodEndUtc <= nowUtc;
    }
}
