using FoodDiary.Application.Abstractions.Notifications.Models;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Notifications.Common;

public interface INotificationReadRepository {
    Task<IReadOnlyList<Notification>> GetByUserAsync(UserId userId, int limit = 50, CancellationToken cancellationToken = default);

    async Task<IReadOnlyList<NotificationReadModel>> GetByUserReadModelsAsync(
        UserId userId,
        int limit = 50,
        CancellationToken cancellationToken = default) {
        IReadOnlyList<Notification> notifications = await GetByUserAsync(userId, limit, cancellationToken).ConfigureAwait(false);
        return [.. notifications.Select(static notification => new NotificationReadModel(
            notification.Id.Value,
            notification.Type,
            notification.ReferenceId,
            notification.PayloadJson,
            notification.IsRead,
            notification.CreatedOnUtc))];
    }

    Task<bool> ExistsAsync(UserId userId, string type, string referenceId, CancellationToken cancellationToken = default);

    Task<int> GetUnreadCountAsync(UserId userId, CancellationToken cancellationToken = default);

    Task<int> GetUnreadCountAsync(UserId userId, string type, CancellationToken cancellationToken = default);
}
