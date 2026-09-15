using FoodDiary.Results;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Notifications.Application.Common;

public interface INotificationUserContextService {
    Task<Result<NotificationUserContext>> GetAsync(UserId userId, CancellationToken cancellationToken = default);
}
