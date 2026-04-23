using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Notifications.Common;

public interface INotificationRepository {
    Task<IReadOnlyList<Notification>> GetByUserAsync(UserId userId, int limit = 50, CancellationToken cancellationToken = default);
    Task<Notification?> GetByIdAsync(NotificationId id, bool asTracking = false, CancellationToken cancellationToken = default);
    Task<Notification> AddAsync(Notification notification, CancellationToken cancellationToken = default);
    Task UpdateAsync(Notification notification, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(UserId userId, string type, string referenceId, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(UserId userId, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(UserId userId, string type, CancellationToken cancellationToken = default);
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
