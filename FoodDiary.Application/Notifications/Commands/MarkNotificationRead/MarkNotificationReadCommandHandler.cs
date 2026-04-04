using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Notifications.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Notifications.Commands.MarkNotificationRead;

public class MarkNotificationReadCommandHandler(
    INotificationRepository notificationRepository)
    : ICommandHandler<MarkNotificationReadCommand, Result> {
    public async Task<Result> Handle(MarkNotificationReadCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId!.Value);
        var notificationId = new NotificationId(command.NotificationId);

        var notification = await notificationRepository.GetByIdAsync(
            notificationId, asTracking: true, cancellationToken: cancellationToken);

        if (notification is null || notification.UserId != userId) {
            return Result.Failure(Errors.Dietologist.InvitationNotFound);
        }

        notification.MarkAsRead();
        await notificationRepository.UpdateAsync(notification, cancellationToken);
        return Result.Success();
    }
}
