using FoodDiary.Modules.Notifications.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Notifications.Application.Abstractions.Common;

public interface INotificationReadStatusRepository {
    Task<IReadOnlyList<Guid>> GetReadIdsAsync(
        UserId userId,
        IReadOnlyCollection<NotificationId> notificationIds,
        CancellationToken cancellationToken);
}
