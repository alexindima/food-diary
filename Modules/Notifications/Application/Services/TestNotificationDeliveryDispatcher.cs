using FoodDiary.Modules.Notifications.Application.Abstractions.Common;
using FoodDiary.Modules.Notifications.Application.Commands.DeliverTestNotification;
using FoodDiary.Mediator;
using FoodDiary.Results;

namespace FoodDiary.Modules.Notifications.Application.Services;

public sealed class TestNotificationDeliveryDispatcher(ISender sender) : ITestNotificationDeliveryDispatcher {
    public async Task DispatchAsync(Guid userId, string type, CancellationToken cancellationToken = default) {
        Result result = await sender.Send(new DeliverTestNotificationCommand(userId, type), cancellationToken).ConfigureAwait(false);
        if (result.IsFailure) {
            throw new InvalidOperationException($"Test notification delivery failed: {result.Error.Code}.");
        }
    }
}
