namespace FoodDiary.Application.Abstractions.Notifications.Common;

public interface INotificationWebPushOutboxProcessor {
    Task<int> ProcessDueAsync(int batchSize, CancellationToken cancellationToken = default);
}
