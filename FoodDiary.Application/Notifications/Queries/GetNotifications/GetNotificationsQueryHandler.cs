using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Notifications.Common;
using FoodDiary.Application.Notifications.Mappings;
using FoodDiary.Application.Notifications.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Notifications.Queries.GetNotifications;

public class GetNotificationsQueryHandler(
    INotificationRepository notificationRepository,
    IUserRepository userRepository)
    : IQueryHandler<GetNotificationsQuery, Result<IReadOnlyList<NotificationModel>>> {
    public async Task<Result<IReadOnlyList<NotificationModel>>> Handle(
        GetNotificationsQuery query, CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<IReadOnlyList<NotificationModel>>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(query.UserId!.Value);
        var accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken);
        if (accessError is not null) {
            return Result.Failure<IReadOnlyList<NotificationModel>>(accessError);
        }

        var notifications = await notificationRepository.GetByUserAsync(userId, cancellationToken: cancellationToken);
        var models = notifications.Select(n => n.ToModel()).ToList();
        return Result.Success<IReadOnlyList<NotificationModel>>(models);
    }
}
