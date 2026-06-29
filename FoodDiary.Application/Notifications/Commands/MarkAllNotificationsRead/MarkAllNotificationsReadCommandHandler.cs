using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Notifications.Commands.MarkAllNotificationsRead;

public class MarkAllNotificationsReadCommandHandler(
    INotificationRepository notificationRepository,
    IUserRepository userRepository,
    INotificationPusher notificationPusher,
    IPostCommitActionQueue postCommitActionQueue)
    : ICommandHandler<MarkAllNotificationsReadCommand, Result> {
    public async Task<Result> Handle(MarkAllNotificationsReadCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId!.Value);
        Error? accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure(accessError);
        }

        await notificationRepository.MarkAllReadAsync(userId, cancellationToken).ConfigureAwait(false);
        postCommitActionQueue.Enqueue(async ct => {
            await notificationPusher.PushUnreadCountAsync(userId.Value, 0, ct).ConfigureAwait(false);
            await notificationPusher.PushNotificationsChangedAsync(userId.Value, ct).ConfigureAwait(false);
        });
        return Result.Success();
    }
}
