using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Notifications.Common;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Notifications;

namespace FoodDiary.Application.Notifications.Commands.MarkNotificationRead;

public sealed class MarkNotificationReadCommandHandler(
    INotificationReadRepository notificationReadRepository,
    INotificationWriteRepository notificationWriteRepository,
    ICurrentUserAccessService currentUserAccessService,
    INotificationPusher notificationPusher,
    IPostCommitActionQueue postCommitActionQueue)
    : ICommandHandler<MarkNotificationReadCommand, Result> {
    public async Task<Result> Handle(MarkNotificationReadCommand command, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure(userIdResult);
        }

        UserId userId = userIdResult.Value;
        Result<NotificationId> notificationIdResult = RequiredIdParser.Parse(
            command.NotificationId,
            nameof(command.NotificationId),
            "Notification id must not be empty.",
            value => new NotificationId(value));
        if (notificationIdResult.IsFailure) {
            return RequiredIdParser.ToFailure(notificationIdResult);
        }

        NotificationId notificationId = notificationIdResult.Value;

        Notification? notification = await notificationWriteRepository.GetByIdAsync(
            notificationId, asTracking: true, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (notification is null || notification.UserId != userId) {
            return Result.Failure(Errors.Dietologist.InvitationNotFound);
        }

        notification.MarkAsRead();
        await notificationWriteRepository.UpdateAsync(notification, cancellationToken).ConfigureAwait(false);
        NotificationPostCommitActions.EnqueueUnreadCountPush(
            postCommitActionQueue,
            notificationReadRepository,
            notificationPusher,
            userId);
        return Result.Success();
    }
}
