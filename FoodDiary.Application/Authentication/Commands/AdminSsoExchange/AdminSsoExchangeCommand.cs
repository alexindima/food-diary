using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Contracts.Authentication;

namespace FoodDiary.Application.Authentication.Commands.AdminSsoExchange;

public sealed record AdminSsoExchangeCommand(string Code) : ICommand<Result<AuthenticationResponse>>;
