using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Notifications.Contracts.Common;

public interface INotificationDeduplicationService {
    Task<bool> ExistsAsync(
        UserId userId,
        string type,
        string referenceId,
        CancellationToken cancellationToken = default);
}
