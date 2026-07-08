using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Notifications.Common;

public interface INotificationRepository : INotificationReadRepository, INotificationReadModelRepository, INotificationWriteRepository {
    new Task<int> GetUnreadCountAsync(
        UserId userId,
        CancellationToken cancellationToken = default);

    new Task<int> GetUnreadCountAsync(
        UserId userId,
        string type,
        CancellationToken cancellationToken = default);
}
