using FoodDiary.Application.Notifications.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Notifications.Common;

public interface INotificationFeedReadService {
    Task<IReadOnlyList<NotificationModel>> GetVisibleNotificationsAsync(
        UserId userId,
        NotificationUserContext context,
        CancellationToken cancellationToken);

    Task<int> GetVisibleUnreadCountAsync(
        UserId userId,
        NotificationUserContext context,
        CancellationToken cancellationToken);
}
