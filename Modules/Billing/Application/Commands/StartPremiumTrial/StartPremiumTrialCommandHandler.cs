using FoodDiary.Modules.Billing.Domain.Contracts;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Modules.Billing.Application.Abstractions.Common;
using FoodDiary.Modules.Billing.Application.Abstractions.Models;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Results;
using FoodDiary.Modules.Billing.Application.Common;
using FoodDiary.Modules.Billing.Application.Models;
using FoodDiary.Mediator;
using FoodDiary.Modules.Billing.Domain.Entities;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Billing.Application.Commands.StartPremiumTrial;

public sealed class StartPremiumTrialCommandHandler(
    IUserBillingService billingUserContextService,
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
        Result<UserBillingProfileModel> userResult = await billingUserContextService.GetAccessibleProfileAsync(userId, cancellationToken).ConfigureAwait(false);
        if (userResult.IsFailure) {
            return Result.Failure<BillingOverviewModel>(userResult.Error);
        }

        UserBillingProfileModel user = userResult.Value;
        BillingSubscription? subscription = await billingSubscriptionRepository.GetByUserIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (user.HasPaidPremium || IsPaidPremiumActive(subscription)) {
            return Result.Failure<BillingOverviewModel>(BillingErrors.SubscriptionAlreadyActive);
        }

        if (user.PremiumTrialStartedAtUtc is not null || user.PremiumTrialEndsAtUtc is not null) {
            return Result.Failure<BillingOverviewModel>(BillingErrors.TrialAlreadyUsed);
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

    private bool IsPaidPremiumActive(BillingSubscription? subscription) =>
        BillingPremiumAccessPolicy.GrantsPremiumAccess(subscription?.Status, subscription?.CurrentPeriodEndUtc, dateTimeProvider.GetUtcNow().UtcDateTime);
}
