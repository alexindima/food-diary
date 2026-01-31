using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Contracts.Authentication;

namespace FoodDiary.Application.Authentication.Commands.TelegramVerify;

public sealed record TelegramVerifyCommand(string InitData) : ICommand<Result<AuthenticationResponse>>;
