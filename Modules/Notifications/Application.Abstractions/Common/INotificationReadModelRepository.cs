using FoodDiary.Modules.Notifications.Application.Abstractions.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Notifications.Application.Abstractions.Common;

public interface INotificationReadModelRepository {
    Task<IReadOnlyList<NotificationReadModel>> GetByUserReadModelsAsync(
        UserId userId,
        int limit = 50,
        CancellationToken cancellationToken = default);

    Task<int> GetUnreadCountAsync(
        UserId userId,
        CancellationToken cancellationToken = default);

    Task<int> GetUnreadCountAsync(
        UserId userId,
        string type,
        CancellationToken cancellationToken = default);
}
