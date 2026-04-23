using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Notifications.Common;
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
        var count = await notificationRepository.GetUnreadCountAsync(userId, cancellationToken);
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user?.HasPassword == true) {
            count -= await notificationRepository.GetUnreadCountAsync(userId, NotificationTypes.PasswordSetupSuggested, cancellationToken);
        }

        return Result.Success(count);
    }
}
