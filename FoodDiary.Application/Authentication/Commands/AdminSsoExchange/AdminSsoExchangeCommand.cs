using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Authentication.Models;

namespace FoodDiary.Application.Authentication.Commands.AdminSsoExchange;

public sealed record AdminSsoExchangeCommand(string Code) : ICommand<Result<AuthenticationModel>>;
