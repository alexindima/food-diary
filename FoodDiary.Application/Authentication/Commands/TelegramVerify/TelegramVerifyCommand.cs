using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Authentication.Models;
using FoodDiary.Application.Authentication.Models;

namespace FoodDiary.Application.Authentication.Commands.TelegramVerify;

public sealed record TelegramVerifyCommand(
    string InitData,
    AuthenticationClientContext? ClientContext = null) : ICommand<Result<AuthenticationModel>>;
