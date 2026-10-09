using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.CheckpointTelegramOperation;

public sealed record CheckpointTelegramOperationCommand(TelegramOperationId OperationId, TelegramLeaseId LeaseId, string Checkpoint, bool Completed, DateTime NextAttemptAtUtc) : ICommand<Result>;
