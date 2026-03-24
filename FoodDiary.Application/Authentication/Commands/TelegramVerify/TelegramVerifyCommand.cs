using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Authentication.Models;

namespace FoodDiary.Application.Authentication.Commands.TelegramVerify;

public sealed record TelegramVerifyCommand(string InitData) : ICommand<Result<AuthenticationModel>>;
