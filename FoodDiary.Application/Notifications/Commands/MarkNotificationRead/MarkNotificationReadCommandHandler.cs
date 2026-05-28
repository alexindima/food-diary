using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Notifications.Commands.MarkNotificationRead;

public class MarkNotificationReadCommandHandler(
    INotificationRepository notificationRepository,
    IUserRepository userRepository,
    INotificationPusher notificationPusher)
    : ICommandHandler<MarkNotificationReadCommand, Result> {
    public async Task<Result> Handle(MarkNotificationReadCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId!.Value);
        var accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken);
        if (accessError is not null) {
            return Result.Failure(accessError);
        }

        var notificationId = new NotificationId(command.NotificationId);

        var notification = await notificationRepository.GetByIdAsync(
            notificationId, asTracking: true, cancellationToken: cancellationToken);

        if (notification is null || notification.UserId != userId) {
            return Result.Failure(Errors.Dietologist.InvitationNotFound);
        }

        notification.MarkAsRead();
        await notificationRepository.UpdateAsync(notification, cancellationToken);
        var unreadCount = await notificationRepository.GetUnreadCountAsync(userId, cancellationToken);
        await notificationPusher.PushUnreadCountAsync(userId.Value, unreadCount, cancellationToken);
        await notificationPusher.PushNotificationsChangedAsync(userId.Value, cancellationToken);
        return Result.Success();
    }
}
