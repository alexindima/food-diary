using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.RegisterTelegramOperation;

public sealed record RegisterTelegramOperationCommand(long UpdateId, long TelegramUserId, string Payload) : ICommand<Result<Guid>>;
