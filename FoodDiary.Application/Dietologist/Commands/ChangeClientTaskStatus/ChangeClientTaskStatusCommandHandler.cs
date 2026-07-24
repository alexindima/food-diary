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
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Dietologist.Commands.ChangeClientTaskStatus;

public sealed class ChangeClientTaskStatusCommandHandler(
    IClientTaskRepository taskRepository,
    IDietologistInvitationReadModelRepository invitationRepository,
    ICurrentUserAccessService currentUserAccessService,
    INotificationWriter notificationWriter,
    TimeProvider timeProvider)
    : ICommandHandler<ChangeClientTaskStatusCommand, Result<ClientTaskModel>> {
    public async Task<Result<ClientTaskModel>> Handle(
        ChangeClientTaskStatusCommand command,
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
        if (task is null || task.ClientUserId != userIdResult.Value) {
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

        ClientTaskStatus previousStatus = task.Status;
        Result<ClientTaskStatus> statusResult = EnumValueParser.ParseRequired<ClientTaskStatus>(
            command.Status,
            nameof(command.Status),
            "Task status must be Open or Completed.");
        if (statusResult.IsFailure ||
            statusResult.Value is not (ClientTaskStatus.Open or ClientTaskStatus.Completed)) {
            return Result.Failure<ClientTaskModel>(Errors.Validation.Invalid(
                nameof(command.Status),
                "Task status must be Open or Completed."));
        }

        ClientTaskStatus requestedStatus = statusResult.Value;
        if (requestedStatus == ClientTaskStatus.Completed) {
            task.Complete();
        } else {
            task.Reopen();
        }

        if (task.Status != previousStatus) {
            await notificationWriter.AddAsync(
                NotificationFactory.CreateClientTaskChanged(
                    task.DietologistUserId,
                    task.ClientUserId.Value.ToString(),
                    forDietologist: true),
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        return Result.Success(task.ToModel(timeProvider.GetUtcNow().UtcDateTime));
    }
}
