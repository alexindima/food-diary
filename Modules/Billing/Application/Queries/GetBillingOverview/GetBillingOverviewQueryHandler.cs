using FoodDiary.Application.Abstractions.Users.Queries.GetUserBillingProfile;
using FoodDiary.Modules.Billing.Domain.Contracts;
using FoodDiary.Modules.Billing.Application.Abstractions.Common;
using FoodDiary.Modules.Billing.Application.Abstractions.Models;
using FoodDiary.Results;
using FoodDiary.Modules.Billing.Application.Common;
using FoodDiary.Mediator;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Modules.Billing.Application.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Billing.Application.Queries.GetBillingOverview;

public sealed class GetBillingOverviewQueryHandler(
    ISender billingUserContextService,
    IBillingSubscriptionReadModelRepository billingSubscriptionRepository,
    IBillingPublicConfigProvider billingPublicConfigProvider,
    TimeProvider dateTimeProvider)
    : IRequestHandler<GetBillingOverviewQuery, Result<BillingOverviewModel>> {
    public async Task<Result<BillingOverviewModel>> Handle(GetBillingOverviewQuery request, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await BillingCurrentUserAccessResolver.ResolveAsync(
            request.UserId,
            billingUserContextService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return BillingCurrentUserAccessResolver.ToFailure<BillingOverviewModel>(userIdResult);
        }

        UserId userId = userIdResult.Value;

        Result<UserBillingProfileModel> userProfileResult = await billingUserContextService.Send(new GetUserBillingProfileQuery(UserId: userId), cancellationToken)
            .ConfigureAwait(false);
        if (userProfileResult.IsFailure) {
            return Result.Failure<BillingOverviewModel>(userProfileResult.Error);
        }

        UserBillingProfileModel userProfile = userProfileResult.Value;
        BillingSubscriptionOverviewReadModel? subscription = await billingSubscriptionRepository
            .GetOverviewReadModelByUserIdAsync(userId, cancellationToken)
            .ConfigureAwait(false);
        DateTime nowUtc = dateTimeProvider.GetUtcNow().UtcDateTime;
        bool isTrialActive = userProfile.PremiumTrialStartedAtUtc <= nowUtc && userProfile.PremiumTrialEndsAtUtc > nowUtc;
        bool hasUsedTrial = userProfile.PremiumTrialStartedAtUtc.HasValue || userProfile.PremiumTrialEndsAtUtc.HasValue;
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
        bool canStartTrial = !hasPaidPremium && !paidSubscriptionActive && !hasUsedTrial;

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
            userProfile.PremiumTrialStartedAtUtc,
            userProfile.PremiumTrialEndsAtUtc,
            isTrialActive,
            hasUsedTrial,
            canStartTrial,
            publicConfig.Provider,
            publicConfig.PaddleClientToken,
            publicConfig.AvailableProviders));
    }

    private static bool IsPaidPremiumActive(BillingSubscriptionOverviewReadModel? subscription, DateTime nowUtc) =>
        BillingPremiumAccessPolicy.GrantsPremiumAccess(subscription?.Status, subscription?.CurrentPeriodEndUtc, nowUtc);

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
        UserBillingProfileModel userProfile,
        bool isTrialActive,
        bool providerTrialExpired) {
        if (providerTrialExpired) {
            return isTrialActive ? userProfile.PremiumTrialStartedAtUtc : null;
        }

        return subscription?.CurrentPeriodStartUtc ?? (isTrialActive ? userProfile.PremiumTrialStartedAtUtc : null);
    }

    private static DateTime? ResolveCurrentPeriodEndUtc(
        BillingSubscriptionOverviewReadModel? subscription,
        UserBillingProfileModel userProfile,
        bool isTrialActive,
        bool providerTrialExpired) {
        if (providerTrialExpired) {
            return isTrialActive ? userProfile.PremiumTrialEndsAtUtc : null;
        }

        return subscription?.CurrentPeriodEndUtc ?? (isTrialActive ? userProfile.PremiumTrialEndsAtUtc : null);
    }
}
