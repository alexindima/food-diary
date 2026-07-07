using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Notifications.Common;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Notifications.Commands.MarkAllNotificationsRead;

public sealed class MarkAllNotificationsReadCommandHandler(
    INotificationReadRepository notificationReadRepository,
    INotificationWriteRepository notificationWriteRepository,
    ICurrentUserAccessService currentUserAccessService,
    INotificationPusher notificationPusher,
    IPostCommitActionQueue postCommitActionQueue)
    : ICommandHandler<MarkAllNotificationsReadCommand, Result> {
    public async Task<Result> Handle(MarkAllNotificationsReadCommand command, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure(userIdResult);
        }

        UserId userId = userIdResult.Value;
        await notificationWriteRepository.MarkAllReadAsync(userId, cancellationToken).ConfigureAwait(false);
        NotificationPostCommitActions.EnqueueUnreadCountPush(
            postCommitActionQueue,
            notificationReadRepository,
            notificationPusher,
            userId);
        return Result.Success();
    }
}
