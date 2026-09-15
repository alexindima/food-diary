using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Identity.Application.Authentication.Models;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.TelegramVerify;

public sealed record TelegramVerifyCommand(
    string InitData,
    AuthenticationClientContext? ClientContext = null) : ICommand<Result<AuthenticationModel>>;
