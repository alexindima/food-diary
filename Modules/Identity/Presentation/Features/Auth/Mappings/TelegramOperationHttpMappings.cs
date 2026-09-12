using FoodDiary.Application.Identity.Authentication.Commands.AcquireTelegramOperation;
using FoodDiary.Application.Identity.Authentication.Commands.CheckpointTelegramOperation;
using FoodDiary.Application.Identity.Authentication.Commands.RegisterTelegramOperation;
using FoodDiary.Application.Identity.Authentication.Queries.ListReadyTelegramOperations;
using FoodDiary.Presentation.Api.Features.Auth.Requests;

namespace FoodDiary.Presentation.Api.Features.Auth.Mappings;

public static class TelegramOperationHttpMappings {
    public static RegisterTelegramOperationCommand ToCommand(this RegisterTelegramOperationHttpRequest request) =>
        new(request.UpdateId, request.TelegramUserId, request.Payload);

    public static ListReadyTelegramOperationsQuery ToReadyQuery() => new();

    public static AcquireTelegramOperationCommand ToAcquireCommand(this Guid operationId) => new(operationId);

    public static CheckpointTelegramOperationCommand ToCommand(this CheckpointTelegramOperationHttpRequest request, Guid operationId) =>
        new(operationId, request.LeaseId, request.Checkpoint, request.Completed, request.NextAttemptAtUtc);
}
