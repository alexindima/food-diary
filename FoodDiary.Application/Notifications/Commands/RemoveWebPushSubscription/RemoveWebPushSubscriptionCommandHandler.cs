using FoodDiary.Application.Abstractions.Common.Abstractions.Audit;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Notifications.Commands.RemoveWebPushSubscription;

public sealed class RemoveWebPushSubscriptionCommandHandler(
    IWebPushSubscriptionRepository webPushSubscriptionRepository,
    IUserRepository userRepository,
    IAuditLogger auditLogger)
    : ICommandHandler<RemoveWebPushSubscriptionCommand, Result> {
    public async Task<Result> Handle(RemoveWebPushSubscriptionCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId.Value);
        var accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken);
        if (accessError is not null) {
            return Result.Failure(accessError);
        }

        if (string.IsNullOrWhiteSpace(command.Endpoint)) {
            return Result.Success();
        }

        var existing = await webPushSubscriptionRepository.GetByEndpointAsync(
            command.Endpoint,
            asTracking: true,
            cancellationToken);
        if (existing is null || existing.UserId != userId) {
            return Result.Success();
        }

        await webPushSubscriptionRepository.DeleteAsync(existing, cancellationToken);
        auditLogger.Log(
            "notifications.push-subscription.disconnected",
            userId,
            "WebPushSubscription",
            existing.Id.Value.ToString(),
            $"endpointHost={GetEndpointHost(existing.Endpoint)}");
        return Result.Success();
    }

    private static string GetEndpointHost(string endpoint) {
        return Uri.TryCreate(endpoint, UriKind.Absolute, out var uri)
            ? uri.Host
            : endpoint;
    }
}
