using FoodDiary.Results;
using FoodDiary.Application.Notifications.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Notifications.Services;

internal sealed class NotificationUserAccessService(IUserContextService userContextService) : INotificationUserAccessService {
    public Task<Result<User>> GetAccessibleUserAsync(UserId userId, CancellationToken cancellationToken) =>
        userContextService.GetAccessibleUserAsync(userId, cancellationToken);

    public Task<Error?> EnsureCanAccessAsync(UserId userId, CancellationToken cancellationToken = default) =>
        userContextService.EnsureCanAccessAsync(userId, cancellationToken);

    public Task UpdateUserAsync(User user, CancellationToken cancellationToken) =>
        userContextService.UpdateUserAsync(user, cancellationToken);
}
