using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Notifications.Common;

public interface INotificationWriteRepository {
    Task<Notification?> GetByIdAsync(NotificationId id, bool asTracking = false, CancellationToken cancellationToken = default);

    Task<Notification> AddAsync(Notification notification, CancellationToken cancellationToken = default);

    Task UpdateAsync(Notification notification, CancellationToken cancellationToken = default);

    Task MarkAllReadAsync(UserId userId, CancellationToken cancellationToken = default);

    Task<int> DeleteExpiredBatchAsync(
        IReadOnlyCollection<string> transientTypes,
        DateTime transientReadOlderThanUtc,
        DateTime transientUnreadOlderThanUtc,
        DateTime standardReadOlderThanUtc,
        DateTime standardUnreadOlderThanUtc,
        int batchSize,
        CancellationToken cancellationToken = default);
}
