using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.AcquireTelegramOperation;

public sealed record AcquireTelegramOperationCommand(Guid OperationId) : ICommand<Result<TelegramOperationLease>>;
