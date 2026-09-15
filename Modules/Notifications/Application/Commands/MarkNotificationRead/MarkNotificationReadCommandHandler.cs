using FoodDiary.Modules.Users.Contracts.Common.Validation;
using FoodDiary.Modules.Notifications.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Results;
using FoodDiary.Modules.Notifications.Application.Common;
using FoodDiary.Modules.Notifications.Application.Abstractions.Common;
using FoodDiary.Modules.Notifications.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Notifications.Domain.Entities;

namespace FoodDiary.Modules.Notifications.Application.Commands.MarkNotificationRead;

public sealed class MarkNotificationReadCommandHandler(
    INotificationWriteRepository notificationWriteRepository,
    ICurrentUserAccessService currentUserAccessService,
    INotificationClientRefreshService notificationClientRefreshService,
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
        Result<NotificationId> notificationIdResult = NotificationIdParser.Parse(
            command.NotificationId,
            nameof(command.NotificationId),
            "Notification id must not be empty.",
            value => new NotificationId(value));
        if (notificationIdResult.IsFailure) {
            return NotificationIdParser.ToFailure(notificationIdResult);
        }

        NotificationId notificationId = notificationIdResult.Value;

        Notification? notification = await notificationWriteRepository.GetByIdAsync(
            notificationId, asTracking: true, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (notification is null || notification.UserId != userId) {
            return Result.Failure(NotificationErrors.LegacyNotFound);
        }

        notification.MarkAsRead();
        await notificationWriteRepository.UpdateAsync(notification, cancellationToken).ConfigureAwait(false);
        NotificationPostCommitActions.EnqueueUnreadCountPush(
            postCommitActionQueue,
            notificationClientRefreshService,
            userId);
        return Result.Success();
    }
}
