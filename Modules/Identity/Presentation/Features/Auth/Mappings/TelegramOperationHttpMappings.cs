using FoodDiary.Modules.Identity.Application.Authentication.Commands.AcquireTelegramOperation;
using FoodDiary.Modules.Identity.Application.Authentication.Commands.CheckpointTelegramOperation;
using FoodDiary.Modules.Identity.Application.Authentication.Commands.RegisterTelegramOperation;
using FoodDiary.Modules.Identity.Application.Authentication.Queries.ListReadyTelegramOperations;
using FoodDiary.Modules.Identity.Presentation.Features.Auth.Requests;

namespace FoodDiary.Modules.Identity.Presentation.Features.Auth.Mappings;

public static class TelegramOperationHttpMappings {
    public static RegisterTelegramOperationCommand ToCommand(this RegisterTelegramOperationHttpRequest request) =>
        new(request.UpdateId, request.TelegramUserId, request.Payload);

    public static ListReadyTelegramOperationsQuery ToReadyQuery() => new();

    public static AcquireTelegramOperationCommand ToAcquireCommand(this Guid operationId) => new(operationId);

    public static CheckpointTelegramOperationCommand ToCommand(this CheckpointTelegramOperationHttpRequest request, Guid operationId) =>
        new(operationId, request.LeaseId, request.Checkpoint, request.Completed, request.NextAttemptAtUtc);
}
