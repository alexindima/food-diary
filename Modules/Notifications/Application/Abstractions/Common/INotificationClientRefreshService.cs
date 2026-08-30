using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Notifications.Common;

public interface INotificationClientRefreshService {
    Task RefreshAsync(
        UserId userId,
        bool pushChanged,
        CancellationToken cancellationToken);
}
