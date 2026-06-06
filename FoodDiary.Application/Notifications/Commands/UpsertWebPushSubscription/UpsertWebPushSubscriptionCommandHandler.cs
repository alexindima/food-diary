using FoodDiary.Application.Abstractions.Common.Abstractions.Audit;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Notifications.Commands.UpsertWebPushSubscription;

public sealed class UpsertWebPushSubscriptionCommandHandler(
    IWebPushSubscriptionRepository webPushSubscriptionRepository,
    IUserRepository userRepository,
    IAuditLogger auditLogger)
    : ICommandHandler<UpsertWebPushSubscriptionCommand, Result> {
    public async Task<Result> Handle(UpsertWebPushSubscriptionCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId.Value);
        Error? accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure(accessError);
        }

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
                $"endpointHost={GetEndpointHost(command.Endpoint)};locale={command.Locale ?? "-"}");
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
            $"endpointHost={GetEndpointHost(existing.Endpoint)};locale={command.Locale ?? "-"}");
        return Result.Success();
    }

    private static string GetEndpointHost(string endpoint) {
        return Uri.TryCreate(endpoint, UriKind.Absolute, out Uri? uri)
            ? uri.Host
            : endpoint;
    }
}
