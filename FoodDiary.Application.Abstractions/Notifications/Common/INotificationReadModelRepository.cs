using FoodDiary.Application.Abstractions.Notifications.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Notifications.Common;

public interface INotificationReadModelRepository {
    Task<IReadOnlyList<NotificationReadModel>> GetByUserReadModelsAsync(
        UserId userId,
        int limit = 50,
        CancellationToken cancellationToken = default);
}
