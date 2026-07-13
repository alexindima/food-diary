using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Notifications.Commands.DeliverTestNotification;
using FoodDiary.Mediator;

namespace FoodDiary.Application.Notifications.Services;

public sealed class TestNotificationDeliveryDispatcher(ISender sender) : ITestNotificationDeliveryDispatcher {
    public async Task DispatchAsync(Guid userId, string type, CancellationToken cancellationToken = default) {
        await sender.Send(new DeliverTestNotificationCommand(userId, type), cancellationToken).ConfigureAwait(false);
    }
}
