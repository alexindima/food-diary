using FoodDiary.Application.Abstractions.Common.Abstractions.Audit;
using FoodDiary.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Notifications.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Notifications.Commands.UpsertWebPushSubscription;

public sealed class UpsertWebPushSubscriptionCommandHandler(
    IWebPushSubscriptionWriteRepository webPushSubscriptionRepository,
    ICurrentUserAccessService currentUserAccessService,
    IAuditLogger auditLogger)
    : ICommandHandler<UpsertWebPushSubscriptionCommand, Result> {
    public async Task<Result> Handle(UpsertWebPushSubscriptionCommand command, CancellationToken cancellationToken) {
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
            userId,
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
}
