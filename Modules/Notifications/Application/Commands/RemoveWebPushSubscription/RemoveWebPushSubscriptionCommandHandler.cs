using FoodDiary.Modules.Users.Contracts.Common.Validation;
using FoodDiary.Modules.Notifications.Application.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Audit;
using FoodDiary.Results;
using FoodDiary.Modules.Notifications.Application.Abstractions.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Notifications.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Notifications.Application.Commands.RemoveWebPushSubscription;

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
            return UserIdParser.ToFailure(userIdResult);
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
