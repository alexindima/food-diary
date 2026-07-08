using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Notifications.Common;

public interface INotificationUserAccessService : ICurrentUserAccessService {
    Task<Result<User>> GetAccessibleUserAsync(UserId userId, CancellationToken cancellationToken);
    Task UpdateUserAsync(User user, CancellationToken cancellationToken);
}
