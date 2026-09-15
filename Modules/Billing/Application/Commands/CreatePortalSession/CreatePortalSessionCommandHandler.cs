using FoodDiary.Application.Abstractions.Users.Queries.GetUserBillingProfile;
using FoodDiary.Modules.Billing.Application.Abstractions.Common;
using FoodDiary.Modules.Billing.Application.Abstractions.Models;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Modules.Billing.Application.Common;
using FoodDiary.Mediator;
using FoodDiary.Results;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Billing.Application.Commands.CreatePortalSession;

public sealed class CreatePortalSessionCommandHandler(
    ISender billingUserContextService,
    IBillingSubscriptionReadModelRepository billingSubscriptionRepository,
    IBillingProviderGatewayAccessor billingProviderGatewayAccessor)
    : IRequestHandler<CreatePortalSessionCommand, Result<BillingPortalSessionModel>> {
    public async Task<Result<BillingPortalSessionModel>> Handle(
        CreatePortalSessionCommand request,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await BillingCurrentUserAccessResolver.ResolveAsync(
            request.UserId,
            billingUserContextService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return BillingCurrentUserAccessResolver.ToFailure<BillingPortalSessionModel>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        Result<UserBillingProfileModel> userResult = await billingUserContextService.Send(new GetUserBillingProfileQuery(UserId: userId), cancellationToken).ConfigureAwait(false);
        if (userResult.IsFailure) {
            return Result.Failure<BillingPortalSessionModel>(userResult.Error);
        }

        BillingSubscriptionOverviewReadModel? subscription = await billingSubscriptionRepository.GetOverviewReadModelByUserIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (subscription is null || string.IsNullOrWhiteSpace(subscription.ExternalCustomerId)) {
            return Result.Failure<BillingPortalSessionModel>(BillingErrors.CustomerPortalUnavailable);
        }

        IBillingProviderGateway? billingProvider = billingProviderGatewayAccessor.GetProviderOrDefault(subscription.Provider);
        if (billingProvider is null) {
            return Result.Failure<BillingPortalSessionModel>(BillingErrors.CustomerPortalUnavailable);
        }

        Result<BillingPortalSessionModel> sessionResult = await billingProvider.CreatePortalSessionAsync(
            new BillingPortalSessionRequestModel(subscription.ExternalCustomerId),
            cancellationToken).ConfigureAwait(false);
        return sessionResult.IsFailure ? Result.Failure<BillingPortalSessionModel>(sessionResult.Error) : Result.Success(sessionResult.Value);
    }
}
