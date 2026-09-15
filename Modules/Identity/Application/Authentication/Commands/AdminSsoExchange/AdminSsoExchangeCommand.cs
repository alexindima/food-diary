using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Identity.Application.Authentication.Models;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.AdminSsoExchange;

public sealed record AdminSsoExchangeCommand(
    string Code,
    AuthenticationClientContext? ClientContext = null) : ICommand<Result<AuthenticationModel>>;
