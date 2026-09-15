using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.CheckpointTelegramOperation;

public sealed record CheckpointTelegramOperationCommand(Guid OperationId, Guid LeaseId, string Checkpoint, bool Completed, DateTime NextAttemptAtUtc) : ICommand<Result>;
