using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Notifications.Mappings;
using FoodDiary.Application.Notifications.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Entities.Notifications;

namespace FoodDiary.Application.Notifications.Queries.GetNotifications;

public class GetNotificationsQueryHandler(
    INotificationRepository notificationRepository,
    IUserRepository userRepository,
    INotificationTextRenderer notificationTextRenderer)
    : IQueryHandler<GetNotificationsQuery, Result<IReadOnlyList<NotificationModel>>> {
    public async Task<Result<IReadOnlyList<NotificationModel>>> Handle(
        GetNotificationsQuery query, CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<IReadOnlyList<NotificationModel>>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(query.UserId!.Value);
        Error? accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<IReadOnlyList<NotificationModel>>(accessError);
        }

        User? user = await userRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<Notification> notifications = await notificationRepository.GetByUserAsync(userId, cancellationToken: cancellationToken).ConfigureAwait(false);
        IEnumerable<Notification> visibleNotifications = user?.HasPassword == true
            ? notifications.Where(notification => !string.Equals(notification.Type, NotificationTypes.PasswordSetupSuggested, StringComparison.Ordinal))
            : notifications;
        var models = visibleNotifications
            .Select(notification => notification.ToModel(
                notificationTextRenderer.RenderFromPayload(notification.Type, notification.PayloadJson, user?.Language)))
            .ToList();
        return Result.Success<IReadOnlyList<NotificationModel>>(models);
    }
}
