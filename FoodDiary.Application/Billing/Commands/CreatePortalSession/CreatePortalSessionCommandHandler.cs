using FoodDiary.Application.Abstractions.Billing.Common;
using FoodDiary.Application.Abstractions.Billing.Models;
using FoodDiary.Application.Billing.Common;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Billing;

namespace FoodDiary.Application.Billing.Commands.CreatePortalSession;

public sealed class CreatePortalSessionCommandHandler(
    IBillingUserContextService billingUserContextService,
    IBillingSubscriptionRepository billingSubscriptionRepository,
    IBillingProviderGatewayAccessor billingProviderGatewayAccessor)
    : ICommandHandler<CreatePortalSessionCommand, Result<BillingPortalSessionModel>> {
    public async Task<Result<BillingPortalSessionModel>> Handle(
        CreatePortalSessionCommand command,
        CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<BillingPortalSessionModel>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId.Value);
        Result<Domain.Entities.Users.User> userResult = await billingUserContextService.GetAccessibleUserAsync(userId, cancellationToken).ConfigureAwait(false);
        if (userResult.IsFailure) {
            return Result.Failure<BillingPortalSessionModel>(userResult.Error);
        }

        BillingSubscription? subscription = await billingSubscriptionRepository.GetByUserIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (subscription is null || string.IsNullOrWhiteSpace(subscription.ExternalCustomerId)) {
            return Result.Failure<BillingPortalSessionModel>(Errors.Billing.CustomerPortalUnavailable);
        }

        IBillingProviderGateway? billingProvider = billingProviderGatewayAccessor.GetProviderOrDefault(subscription.Provider);
        if (billingProvider is null) {
            return Result.Failure<BillingPortalSessionModel>(Errors.Billing.CustomerPortalUnavailable);
        }

        Result<BillingPortalSessionModel> sessionResult = await billingProvider.CreatePortalSessionAsync(
            new BillingPortalSessionRequestModel(subscription.ExternalCustomerId),
            cancellationToken).ConfigureAwait(false);
        return sessionResult.IsFailure ? Result.Failure<BillingPortalSessionModel>(sessionResult.Error) : Result.Success(sessionResult.Value);
    }
}
