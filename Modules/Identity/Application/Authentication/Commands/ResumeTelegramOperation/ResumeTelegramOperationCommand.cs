using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.ResumeTelegramOperation;

public sealed record ResumeTelegramOperationCommand(TelegramOperationId OperationId, long TelegramUserId, string ExpectedCheckpoint, string Checkpoint)
    : ICommand<Result>;
