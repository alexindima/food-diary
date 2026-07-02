using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Notifications.Common;

public interface INotificationUserContextService {
    Task<Result<NotificationUserContext>> GetAsync(UserId userId, CancellationToken cancellationToken = default);
}
