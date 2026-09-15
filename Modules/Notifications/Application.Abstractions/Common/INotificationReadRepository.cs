using FoodDiary.Modules.Notifications.Domain.Entities;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Notifications.Application.Abstractions.Common;

public interface INotificationReadRepository {
    Task<IReadOnlyList<Notification>> GetByUserAsync(UserId userId, int limit = 50, CancellationToken cancellationToken = default);
}
