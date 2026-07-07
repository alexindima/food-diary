using FoodDiary.Application.Abstractions.Common.Abstractions.Audit;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Notifications.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Notifications.Commands.RemoveWebPushSubscription;

public sealed class RemoveWebPushSubscriptionCommandHandler(
    IWebPushSubscriptionWriteRepository webPushSubscriptionRepository,
    ICurrentUserAccessService currentUserAccessService,
    IAuditLogger auditLogger)
    : ICommandHandler<RemoveWebPushSubscriptionCommand, Result> {
    public async Task<Result> Handle(RemoveWebPushSubscriptionCommand command, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return Result.Failure(userIdResult.Error);
        }

        UserId userId = userIdResult.Value;
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
            $"endpointHost={WebPushEndpointHost.Resolve(existing.Endpoint)}");
        return Result.Success();
    }
}
