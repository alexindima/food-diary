using FoodDiary.Modules.Notifications.Application.Abstractions.Common;
using FoodDiary.Modules.Notifications.Application.Commands.DeliverTestNotification;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.Notifications.Application.Services;

public sealed class TestNotificationDeliveryDispatcher(ISender sender) : ITestNotificationDeliveryDispatcher {
    public async Task DispatchAsync(Guid userId, string type, CancellationToken cancellationToken = default) {
        await sender.Send(new DeliverTestNotificationCommand(userId, type), cancellationToken).ConfigureAwait(false);
    }
}
