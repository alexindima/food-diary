using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Authentication.Models;
using FoodDiary.Application.Identity.Authentication.Models;

namespace FoodDiary.Application.Identity.Authentication.Commands.AdminSsoExchange;

public sealed record AdminSsoExchangeCommand(
    string Code,
    AuthenticationClientContext? ClientContext = null) : ICommand<Result<AuthenticationModel>>;
