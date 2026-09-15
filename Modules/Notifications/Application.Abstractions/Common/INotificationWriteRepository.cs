using FoodDiary.Modules.Notifications.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Notifications.Domain.Entities;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Notifications.Application.Abstractions.Common;

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
