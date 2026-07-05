using FoodDiary.Application.Abstractions.Billing.Common;
using FoodDiary.Application.Abstractions.Billing.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Billing.Common;
using FoodDiary.Application.Billing.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Billing.Services;

public sealed class BillingOverviewReadService(
    IBillingUserContextService billingUserContextService,
    IBillingSubscriptionReadRepository billingSubscriptionRepository,
    IBillingPublicConfigProvider billingPublicConfigProvider,
    TimeProvider dateTimeProvider)
    : IBillingOverviewReadService {
    private const string YooKassaProvider = "YooKassa";

    public async Task<Result<BillingOverviewModel>> GetAsync(UserId userId, CancellationToken cancellationToken) {
        Result<BillingUserProfileModel> userProfileResult = await billingUserContextService
            .GetAccessibleUserProfileAsync(userId, cancellationToken)
            .ConfigureAwait(false);
        if (userProfileResult.IsFailure) {
            return Result.Failure<BillingOverviewModel>(userProfileResult.Error);
        }

        BillingUserProfileModel userProfile = userProfileResult.Value;
        BillingSubscriptionOverviewReadModel? subscription = await billingSubscriptionRepository
            .GetOverviewReadModelByUserIdAsync(userId, cancellationToken)
            .ConfigureAwait(false);
        DateTime nowUtc = dateTimeProvider.GetUtcNow().UtcDateTime;
        bool isTrialActive = userProfile.HasActivePremiumTrial(nowUtc);
        bool hasPaidPremium = userProfile.HasPaidPremium;
        bool paidSubscriptionActive = IsPaidPremiumActive(subscription, nowUtc);
        bool isPremium = hasPaidPremium || paidSubscriptionActive || isTrialActive;
        bool providerTrialExpired = IsExpiredProviderTrial(subscription, nowUtc);
        string? subscriptionStatus = ResolveSubscriptionStatus(subscription, isTrialActive, providerTrialExpired);
        DateTime? currentPeriodStartUtc = ResolveCurrentPeriodStartUtc(subscription, userProfile, isTrialActive, providerTrialExpired);
        DateTime? currentPeriodEndUtc = ResolveCurrentPeriodEndUtc(subscription, userProfile, isTrialActive, providerTrialExpired);
        BillingPublicConfigModel publicConfig = billingPublicConfigProvider.GetPublicConfig();
        bool renewalEnabled = subscription?.NextBillingAttemptUtc is not null &&
            !subscription.CancelAtPeriodEnd;
        bool canStartTrial = !hasPaidPremium && !paidSubscriptionActive && !userProfile.HasUsedPremiumTrial();

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
                !string.Equals(subscription.Provider, YooKassaProvider, StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(subscription.ExternalCustomerId),
            userProfile.PremiumTrialStartedAtUtc,
            userProfile.PremiumTrialEndsAtUtc,
            isTrialActive,
            userProfile.HasUsedPremiumTrial(),
            canStartTrial,
            publicConfig.Provider,
            publicConfig.PaddleClientToken,
            publicConfig.AvailableProviders));
    }

    private static bool IsPaidPremiumActive(BillingSubscriptionOverviewReadModel? subscription, DateTime nowUtc) {
        if (subscription is null || string.IsNullOrWhiteSpace(subscription.Status)) {
            return false;
        }

        return subscription.Status.Trim().ToLowerInvariant() switch {
            "trialing" => subscription.CurrentPeriodEndUtc.HasValue && subscription.CurrentPeriodEndUtc > nowUtc,
            "active" => true,
            "past_due" => !subscription.CurrentPeriodEndUtc.HasValue || subscription.CurrentPeriodEndUtc > nowUtc,
            _ => false,
        };
    }

    private static bool IsExpiredProviderTrial(BillingSubscriptionOverviewReadModel? subscription, DateTime nowUtc) {
        if (subscription is null ||
            !string.Equals(subscription.Status, "trialing", StringComparison.OrdinalIgnoreCase)) {
            return false;
        }

        return !subscription.CurrentPeriodEndUtc.HasValue || subscription.CurrentPeriodEndUtc <= nowUtc;
    }

    private static string? ResolveSubscriptionStatus(
        BillingSubscriptionOverviewReadModel? subscription,
        bool isTrialActive,
        bool providerTrialExpired) {
        if (providerTrialExpired) {
            return isTrialActive ? "trialing" : null;
        }

        return subscription?.Status ?? (isTrialActive ? "trialing" : null);
    }

    private static DateTime? ResolveCurrentPeriodStartUtc(
        BillingSubscriptionOverviewReadModel? subscription,
        BillingUserProfileModel userProfile,
        bool isTrialActive,
        bool providerTrialExpired) {
        if (providerTrialExpired) {
            return isTrialActive ? userProfile.PremiumTrialStartedAtUtc : null;
        }

        return subscription?.CurrentPeriodStartUtc ?? (isTrialActive ? userProfile.PremiumTrialStartedAtUtc : null);
    }

    private static DateTime? ResolveCurrentPeriodEndUtc(
        BillingSubscriptionOverviewReadModel? subscription,
        BillingUserProfileModel userProfile,
        bool isTrialActive,
        bool providerTrialExpired) {
        if (providerTrialExpired) {
            return isTrialActive ? userProfile.PremiumTrialEndsAtUtc : null;
        }

        return subscription?.CurrentPeriodEndUtc ?? (isTrialActive ? userProfile.PremiumTrialEndsAtUtc : null);
    }
}
