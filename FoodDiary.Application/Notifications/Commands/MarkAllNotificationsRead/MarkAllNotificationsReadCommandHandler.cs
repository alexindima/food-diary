using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Notifications.Commands.MarkAllNotificationsRead;

public class MarkAllNotificationsReadCommandHandler(
    INotificationRepository notificationRepository)
    : ICommandHandler<MarkAllNotificationsReadCommand, Result> {
    public async Task<Result> Handle(MarkAllNotificationsReadCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId!.Value);
        await notificationRepository.MarkAllReadAsync(userId, cancellationToken);
        return Result.Success();
    }
}
