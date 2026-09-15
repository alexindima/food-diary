using FoodDiary.Results;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Notifications.Application.Common;

public interface INotificationUserContextService {
    Task<Result<NotificationUserContext>> GetAsync(UserId userId, CancellationToken cancellationToken = default);
}
