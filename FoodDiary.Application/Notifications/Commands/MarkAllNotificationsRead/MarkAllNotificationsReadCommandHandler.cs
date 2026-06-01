using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Notifications.Commands.MarkAllNotificationsRead;

public class MarkAllNotificationsReadCommandHandler(
    INotificationRepository notificationRepository,
    IUserRepository userRepository,
    INotificationPusher notificationPusher)
    : ICommandHandler<MarkAllNotificationsReadCommand, Result> {
    public async Task<Result> Handle(MarkAllNotificationsReadCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId!.Value);
        var accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure(accessError);
        }

        await notificationRepository.MarkAllReadAsync(userId, cancellationToken).ConfigureAwait(false);
        await notificationPusher.PushUnreadCountAsync(userId.Value, 0, cancellationToken).ConfigureAwait(false);
        await notificationPusher.PushNotificationsChangedAsync(userId.Value, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
