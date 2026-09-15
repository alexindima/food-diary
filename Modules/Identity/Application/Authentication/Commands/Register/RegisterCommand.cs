using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Identity.Application.Authentication.Models;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.Register;

public record RegisterCommand(
    string Email,
    string Password,
    string? Language,
    string? ClientOrigin = null,
    AuthenticationClientContext? ClientContext = null
) : ICommand<Result<AuthenticationModel>>;
