using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Authentication.Models;
using FoodDiary.Application.Authentication.Models;

namespace FoodDiary.Application.Authentication.Commands.TelegramVerify;

public sealed record TelegramVerifyCommand(
    string InitData,
    AuthenticationClientContext? ClientContext = null) : ICommand<Result<AuthenticationModel>>;
