using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Notifications.Common;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Notifications;

namespace FoodDiary.Application.Notifications.Commands.MarkNotificationRead;

public class MarkNotificationReadCommandHandler(
    INotificationRepository notificationRepository,
    ICurrentUserAccessService currentUserAccessService,
    INotificationPusher notificationPusher,
    IPostCommitActionQueue postCommitActionQueue)
    : ICommandHandler<MarkNotificationReadCommand, Result> {
    public async Task<Result> Handle(MarkNotificationReadCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId!.Value);
        Error? accessError = await currentUserAccessService.EnsureCanAccessAsync(userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure(accessError);
        }

        var notificationId = new NotificationId(command.NotificationId);

        Notification? notification = await notificationRepository.GetByIdAsync(
            notificationId, asTracking: true, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (notification is null || notification.UserId != userId) {
            return Result.Failure(Errors.Dietologist.InvitationNotFound);
        }

        notification.MarkAsRead();
        await notificationRepository.UpdateAsync(notification, cancellationToken).ConfigureAwait(false);
        NotificationPostCommitActions.EnqueueUnreadCountPush(
            postCommitActionQueue,
            notificationRepository,
            notificationPusher,
            userId);
        return Result.Success();
    }
}
