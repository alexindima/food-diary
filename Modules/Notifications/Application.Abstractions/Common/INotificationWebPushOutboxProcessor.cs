namespace FoodDiary.Modules.Notifications.Application.Abstractions.Common;

public interface INotificationWebPushOutboxProcessor {
    Task<int> ProcessDueAsync(int batchSize, CancellationToken cancellationToken = default);
}
