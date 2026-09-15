using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Identity.Application.Authentication.Models;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.TelegramBotAuth;

public sealed record TelegramBotAuthCommand(
    long TelegramUserId,
    AuthenticationClientContext? ClientContext = null) : ICommand<Result<AuthenticationModel>>;
