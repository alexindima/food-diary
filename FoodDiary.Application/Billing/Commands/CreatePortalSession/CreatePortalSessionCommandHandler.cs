using FoodDiary.Application.Billing.Common;
using FoodDiary.Application.Billing.Models;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Billing.Commands.CreatePortalSession;

public sealed class CreatePortalSessionCommandHandler(
    IUserRepository userRepository,
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
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        var accessError = CurrentUserAccessPolicy.EnsureCanAccess(user);
        if (accessError is not null) {
            return Result.Failure<BillingPortalSessionModel>(accessError);
        }

        var subscription = await billingSubscriptionRepository.GetByUserIdAsync(userId, cancellationToken);
        var billingProvider = billingProviderGatewayAccessor.GetActiveProvider();
        if (subscription is null ||
            !string.Equals(subscription.Provider, billingProvider.Provider, StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(subscription.ExternalCustomerId)) {
            return Result.Failure<BillingPortalSessionModel>(Errors.Billing.CustomerPortalUnavailable);
        }

        var sessionResult = await billingProvider.CreatePortalSessionAsync(
            new BillingPortalSessionRequestModel(subscription.ExternalCustomerId),
            cancellationToken);
        if (sessionResult.IsFailure) {
            return Result.Failure<BillingPortalSessionModel>(sessionResult.Error);
        }

        return Result.Success(sessionResult.Value);
    }
}
