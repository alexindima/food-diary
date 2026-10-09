using FoodDiary.Modules.Users.Contracts.Common.Validation;
using FoodDiary.Authentication.Contracts.Authentication.Common;
using FoodDiary.Application.Contracts.Common.Abstractions.Persistence;
using FoodDiary.Modules.Notifications.Contracts.Common;
using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
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
        NotificationIntent intent = command.Type switch {
            NotificationTypes.FastingCheckInReminder => NotificationIntent.FastingCheckInReminder(referenceId),
            NotificationTypes.EatingWindowStarted => NotificationIntent.EatingWindowStarted(new FastingPhaseNotificationPayload("Intermittent", "EatingWindow"), referenceId),
            NotificationTypes.FastingWindowStarted => NotificationIntent.FastingWindowStarted(new FastingPhaseNotificationPayload("Intermittent", "FastingWindow"), referenceId),
            _ => NotificationIntent.FastingCompleted(new FastingPhaseNotificationPayload("Extended", "FastDay"), referenceId),
        };

        var request = new NotificationRequest(userId, intent);
        await notificationWriter.AddAsync(request, sendWebPush: true, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await clientRefreshService.RefreshAsync(userId, pushChanged: true, cancellationToken).ConfigureAwait(false);
        await postCommitActionQueue.FlushAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
