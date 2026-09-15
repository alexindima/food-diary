using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Modules.Notifications.Contracts.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Notifications.Application.Common;
using FoodDiary.Modules.Notifications.Domain.Entities;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Notifications.Application.Commands.DeliverTestNotification;

public sealed class DeliverTestNotificationCommandHandler(
    INotificationWriter notificationWriter,
    INotificationClientRefreshService clientRefreshService,
    IUnitOfWork unitOfWork,
    IPostCommitActionQueue postCommitActionQueue)
    : ICommandHandler<DeliverTestNotificationCommand, Result> {
    public async Task<Result> Handle(DeliverTestNotificationCommand command, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(command.UserId, AuthenticationErrors.InvalidToken);
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure(userIdResult);
        }

        UserId userId = userIdResult.Value;
        string referenceId = $"test-notification:{command.Type}:{Guid.NewGuid():N}";
        Notification notification = command.Type switch {
            NotificationTypes.FastingCheckInReminder => NotificationFactory.CreateFastingCheckInReminder(userId, referenceId),
            NotificationTypes.EatingWindowStarted => NotificationFactory.CreateEatingWindowStarted(userId, "Intermittent", "EatingWindow", referenceId),
            NotificationTypes.FastingWindowStarted => NotificationFactory.CreateFastingWindowStarted(userId, "Intermittent", "FastingWindow", referenceId),
            _ => NotificationFactory.CreateFastingCompleted(userId, "Extended", "FastDay", referenceId),
        };

        var request = new NotificationRequest(notification.UserId, notification.Type, notification.PayloadJson, notification.ReferenceId);
        await notificationWriter.AddAsync(request, sendWebPush: true, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await clientRefreshService.RefreshAsync(userId, pushChanged: true, cancellationToken).ConfigureAwait(false);
        await postCommitActionQueue.FlushAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
