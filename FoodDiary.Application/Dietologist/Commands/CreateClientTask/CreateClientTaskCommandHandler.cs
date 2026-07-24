using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Application.Notifications.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Dietologist.Commands.CreateClientTask;

public sealed class CreateClientTaskCommandHandler(
    IClientTaskRepository taskRepository,
    IDietologistInvitationReadModelRepository invitationRepository,
    IUserContextService userContextService,
    INotificationWriter notificationWriter,
    TimeProvider timeProvider)
    : ICommandHandler<CreateClientTaskCommand, Result<ClientTaskModel>> {
    public async Task<Result<ClientTaskModel>> Handle(
        CreateClientTaskCommand command,
        CancellationToken cancellationToken) {
        Result<UserId> dietologistIdResult = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId, userContextService, cancellationToken).ConfigureAwait(false);
        if (dietologistIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<ClientTaskModel>(dietologistIdResult);
        }

        Result<UserId> clientIdResult = RequiredIdParser.Parse(
            command.ClientUserId,
            nameof(command.ClientUserId),
            "Client user id must not be empty.",
            value => new UserId(value));
        if (clientIdResult.IsFailure) {
            return Result.Failure<ClientTaskModel>(clientIdResult.Error);
        }

        Result accessResult = await DietologistAccessPolicy.EnsureCanAccessClientReadModelAsync(
            invitationRepository,
            dietologistIdResult.Value,
            clientIdResult.Value,
            cancellationToken).ConfigureAwait(false);
        if (accessResult.IsFailure) {
            return Result.Failure<ClientTaskModel>(accessResult.Error);
        }

        var task = ClientTask.Create(
            dietologistIdResult.Value,
            clientIdResult.Value,
            command.Title,
            command.Details,
            command.DueAtUtc);
        await taskRepository.AddAsync(task, cancellationToken).ConfigureAwait(false);
        await notificationWriter.AddAsync(
            NotificationFactory.CreateClientTaskChanged(
                task.ClientUserId,
                task.ClientUserId.Value.ToString(),
                forDietologist: false),
            cancellationToken: cancellationToken).ConfigureAwait(false);
        return Result.Success(task.ToModel(timeProvider.GetUtcNow().UtcDateTime));
    }
}
