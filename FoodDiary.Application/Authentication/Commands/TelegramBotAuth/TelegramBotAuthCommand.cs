using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Authentication.Models;

namespace FoodDiary.Application.Authentication.Commands.TelegramBotAuth;

public sealed record TelegramBotAuthCommand(long TelegramUserId) : ICommand<Result<AuthenticationModel>>;
