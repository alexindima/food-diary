using FoodDiary.Application.Abstractions.Common.Abstractions.Audit;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Notifications.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Notifications.Commands.UpsertWebPushSubscription;

public sealed class UpsertWebPushSubscriptionCommandHandler(
    IWebPushSubscriptionWriteRepository webPushSubscriptionRepository,
    ICurrentUserAccessService currentUserAccessService,
    IAuditLogger auditLogger)
    : ICommandHandler<UpsertWebPushSubscriptionCommand, Result> {
    public async Task<Result> Handle(UpsertWebPushSubscriptionCommand command, CancellationToken cancellationToken) {
        if (!WebPushEndpointPolicy.IsAllowed(command.Endpoint)) {
            return Result.Failure(Errors.Validation.Invalid(
                nameof(command.Endpoint),
                "Endpoint must be an absolute HTTPS web push URL."));
        }

        if (command.ExpirationTimeUtc is { } expirationTimeUtc &&
            expirationTimeUtc.Kind == DateTimeKind.Unspecified) {
            return Result.Failure(Errors.Validation.Invalid(
                nameof(command.ExpirationTimeUtc),
                "ExpirationTimeUtc timestamp kind must be specified."));
        }

        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure(userIdResult);
        }

        UserId userId = userIdResult.Value;
        WebPushSubscription? existing = await webPushSubscriptionRepository.GetByEndpointAsync(
            command.Endpoint,
            asTracking: true,
            cancellationToken).ConfigureAwait(false);

        if (existing is not null && existing.UserId != userId) {
            return Result.Failure(Errors.Validation.Invalid(
                nameof(command.Endpoint),
                "Endpoint cannot be registered."));
        }

        if (existing is null) {
            await EvictOldestSubscriptionsAsync(userId, cancellationToken).ConfigureAwait(false);
        }

        if (existing is null) {
            var subscription = WebPushSubscription.Create(
                userId,
                command.Endpoint,
                command.P256Dh,
                command.Auth,
                command.ExpirationTimeUtc,
                command.Locale,
                command.UserAgent);

            await webPushSubscriptionRepository.AddAsync(subscription, cancellationToken).ConfigureAwait(false);
            auditLogger.Log(
                "notifications.push-subscription.connected",
                userId,
                "WebPushSubscription",
                subscription.Id.Value.ToString(),
                $"endpointHost={WebPushEndpointHost.Resolve(command.Endpoint)};locale={command.Locale ?? "-"}");
            return Result.Success();
        }

        existing.Refresh(
            command.P256Dh,
            command.Auth,
            command.ExpirationTimeUtc,
            command.Locale,
            command.UserAgent);

        await webPushSubscriptionRepository.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
        auditLogger.Log(
            "notifications.push-subscription.refreshed",
            userId,
            "WebPushSubscription",
            existing.Id.Value.ToString(),
            $"endpointHost={WebPushEndpointHost.Resolve(existing.Endpoint)};locale={command.Locale ?? "-"}");
        return Result.Success();
    }

    private async Task EvictOldestSubscriptionsAsync(UserId userId, CancellationToken cancellationToken) {
        IReadOnlyList<WebPushSubscription> subscriptions = await webPushSubscriptionRepository
            .GetByUserAsync(userId, cancellationToken)
            .ConfigureAwait(false);
        int countToDelete = subscriptions.Count - WebPushDeliveryLimits.MaximumSubscriptionsPerUser + 1;
        if (countToDelete <= 0) {
            return;
        }

        WebPushSubscription[] oldest = [.. subscriptions
            .OrderBy(subscription => subscription.ModifiedOnUtc ?? subscription.CreatedOnUtc)
            .Take(countToDelete)];
        await webPushSubscriptionRepository.DeleteRangeAsync(oldest, cancellationToken).ConfigureAwait(false);
    }
}
