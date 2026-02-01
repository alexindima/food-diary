using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Contracts.Authentication;

namespace FoodDiary.Application.Authentication.Commands.TelegramBotAuth;

public sealed record TelegramBotAuthCommand(long TelegramUserId) : ICommand<Result<AuthenticationResponse>>;
