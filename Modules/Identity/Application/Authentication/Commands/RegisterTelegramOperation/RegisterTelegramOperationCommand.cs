using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.Identity.Authentication.Commands.RegisterTelegramOperation;

public sealed record RegisterTelegramOperationCommand(long UpdateId, long TelegramUserId, string Payload) : ICommand<Result<Guid>>;
