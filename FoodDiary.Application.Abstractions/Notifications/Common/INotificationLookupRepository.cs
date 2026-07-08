using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Notifications.Common;

public interface INotificationLookupRepository {
    Task<bool> ExistsAsync(
        UserId userId,
        string type,
        string referenceId,
        CancellationToken cancellationToken = default);
}
