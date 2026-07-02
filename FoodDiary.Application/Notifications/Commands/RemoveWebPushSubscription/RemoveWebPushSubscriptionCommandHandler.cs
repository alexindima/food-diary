using FoodDiary.Application.Abstractions.Common.Abstractions.Audit;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Notifications.Commands.RemoveWebPushSubscription;

public sealed class RemoveWebPushSubscriptionCommandHandler(
    IWebPushSubscriptionRepository webPushSubscriptionRepository,
    ICurrentUserAccessService currentUserAccessService,
    IAuditLogger auditLogger)
    : ICommandHandler<RemoveWebPushSubscriptionCommand, Result> {
    public async Task<Result> Handle(RemoveWebPushSubscriptionCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId.Value);
        Error? accessError = await currentUserAccessService.EnsureCanAccessAsync(userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure(accessError);
        }

        if (string.IsNullOrWhiteSpace(command.Endpoint)) {
            return Result.Success();
        }

        WebPushSubscription? existing = await webPushSubscriptionRepository.GetByEndpointAsync(
            command.Endpoint,
            asTracking: true,
            cancellationToken).ConfigureAwait(false);
        if (existing is null || existing.UserId != userId) {
            return Result.Success();
        }

        await webPushSubscriptionRepository.DeleteAsync(existing, cancellationToken).ConfigureAwait(false);
        auditLogger.Log(
            "notifications.push-subscription.disconnected",
            userId,
            "WebPushSubscription",
            existing.Id.Value.ToString(),
            $"endpointHost={GetEndpointHost(existing.Endpoint)}");
        return Result.Success();
    }

    private static string GetEndpointHost(string endpoint) {
        return Uri.TryCreate(endpoint, UriKind.Absolute, out Uri? uri)
            ? uri.Host
            : endpoint;
    }
}
