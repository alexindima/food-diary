using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Authentication.Models;
using FoodDiary.Application.Identity.Authentication.Models;

namespace FoodDiary.Application.Identity.Authentication.Commands.Register;

public record RegisterCommand(
    string Email,
    string Password,
    string? Language,
    string? ClientOrigin = null,
    AuthenticationClientContext? ClientContext = null
) : ICommand<Result<AuthenticationModel>>;
