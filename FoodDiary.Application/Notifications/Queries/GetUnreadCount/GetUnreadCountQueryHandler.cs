using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Notifications.Queries.GetUnreadCount;

public class GetUnreadCountQueryHandler(
    INotificationRepository notificationRepository,
    IUserRepository userRepository)
    : IQueryHandler<GetUnreadCountQuery, Result<int>> {
    public async Task<Result<int>> Handle(GetUnreadCountQuery query, CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<int>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(query.UserId!.Value);
        var accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<int>(accessError);
        }

        var count = await notificationRepository.GetUnreadCountAsync(userId, cancellationToken).ConfigureAwait(false);
        var user = await userRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (user?.HasPassword == true) {
            count -= await notificationRepository.GetUnreadCountAsync(userId, NotificationTypes.PasswordSetupSuggested, cancellationToken).ConfigureAwait(false);
        }

        return Result.Success(count);
    }
}
