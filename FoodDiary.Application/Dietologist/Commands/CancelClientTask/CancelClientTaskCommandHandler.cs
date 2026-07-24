using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Application.Notifications.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Dietologist.Commands.CancelClientTask;

public sealed class CancelClientTaskCommandHandler(
    IClientTaskRepository taskRepository,
    IDietologistInvitationReadModelRepository invitationRepository,
    ICurrentUserAccessService currentUserAccessService,
    INotificationWriter notificationWriter,
    TimeProvider timeProvider)
    : ICommandHandler<CancelClientTaskCommand, Result<ClientTaskModel>> {
    public async Task<Result<ClientTaskModel>> Handle(
        CancelClientTaskCommand command,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId, currentUserAccessService, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<ClientTaskModel>(userIdResult);
        }

        Result<ClientTaskId> taskIdResult = RequiredIdParser.Parse(
            command.TaskId,
            nameof(command.TaskId),
            "Task id must not be empty.",
            value => new ClientTaskId(value));
        if (taskIdResult.IsFailure) {
            return Result.Failure<ClientTaskModel>(taskIdResult.Error);
        }

        ClientTask? task = await taskRepository.GetByIdAsync(
            taskIdResult.Value, asTracking: true, cancellationToken).ConfigureAwait(false);
        if (task is null || task.DietologistUserId != userIdResult.Value) {
            return Result.Failure<ClientTaskModel>(Errors.Dietologist.InvitationNotFound);
        }

        Result accessResult = await DietologistAccessPolicy.EnsureCanAccessClientReadModelAsync(
            invitationRepository,
            task.DietologistUserId,
            task.ClientUserId,
            cancellationToken).ConfigureAwait(false);
        if (accessResult.IsFailure) {
            return Result.Failure<ClientTaskModel>(accessResult.Error);
        }

        bool wasCancelled = task.Status == FoodDiary.Domain.Enums.ClientTaskStatus.Cancelled;
        task.Cancel();
        if (!wasCancelled) {
            await notificationWriter.AddAsync(
                NotificationFactory.CreateClientTaskChanged(
                    task.ClientUserId,
                    task.ClientUserId.Value.ToString(),
                    forDietologist: false,
                    cancelled: true),
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        return Result.Success(task.ToModel(timeProvider.GetUtcNow().UtcDateTime));
    }
}
