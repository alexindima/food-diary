using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Authentication.Models;
using FoodDiary.Application.Authentication.Models;

namespace FoodDiary.Application.Authentication.Commands.TelegramBotAuth;

public sealed record TelegramBotAuthCommand(
    long TelegramUserId,
    AuthenticationClientContext? ClientContext = null) : ICommand<Result<AuthenticationModel>>;
