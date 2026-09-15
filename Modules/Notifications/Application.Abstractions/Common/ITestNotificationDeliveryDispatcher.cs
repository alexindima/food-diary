namespace FoodDiary.Modules.Notifications.Application.Abstractions.Common;

public interface ITestNotificationDeliveryDispatcher {
    Task DispatchAsync(Guid userId, string type, CancellationToken cancellationToken = default);
}
