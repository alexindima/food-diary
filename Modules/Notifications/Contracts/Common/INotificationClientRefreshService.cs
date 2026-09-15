using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Notifications.Contracts.Common;

public interface INotificationClientRefreshService {
    Task RefreshAsync(
        UserId userId,
        bool pushChanged,
        CancellationToken cancellationToken);
}
