using FoodDiary.Application.Billing.Common;
using FoodDiary.Application.Billing.Models;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Billing.Queries.GetBillingOverview;

public sealed class GetBillingOverviewQueryHandler(
    IUserRepository userRepository,
    IBillingSubscriptionRepository billingSubscriptionRepository,
    IBillingPublicConfigProvider billingPublicConfigProvider)
    : IQueryHandler<GetBillingOverviewQuery, Result<BillingOverviewModel>> {
    public async Task<Result<BillingOverviewModel>> Handle(
        GetBillingOverviewQuery query,
        CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<BillingOverviewModel>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(query.UserId.Value);
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        var accessError = CurrentUserAccessPolicy.EnsureCanAccess(user);
        if (accessError is not null) {
            return Result.Failure<BillingOverviewModel>(accessError);
        }

        var subscription = await billingSubscriptionRepository.GetByUserIdAsync(userId, cancellationToken);
        var isPremium = user!.HasRole(RoleNames.Premium);
        var publicConfig = billingPublicConfigProvider.GetPublicConfig();

        return Result.Success(new BillingOverviewModel(
            isPremium,
            subscription?.Status,
            subscription?.Plan,
            subscription?.CurrentPeriodEndUtc,
            subscription?.CancelAtPeriodEnd ?? false,
            !string.IsNullOrWhiteSpace(subscription?.ExternalCustomerId),
            publicConfig.Provider,
            publicConfig.PaddleClientToken));
    }
}
