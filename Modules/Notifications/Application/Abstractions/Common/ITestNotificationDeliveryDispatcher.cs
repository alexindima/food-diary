namespace FoodDiary.Application.Abstractions.Notifications.Common;

public interface ITestNotificationDeliveryDispatcher {
    Task DispatchAsync(Guid userId, string type, CancellationToken cancellationToken = default);
}
