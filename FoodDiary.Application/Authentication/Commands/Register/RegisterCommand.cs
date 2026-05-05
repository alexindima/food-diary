using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Authentication.Models;
using FoodDiary.Application.Authentication.Models;

namespace FoodDiary.Application.Authentication.Commands.Register;

public record RegisterCommand(
    string Email,
    string Password,
    string? Language,
    string? ClientOrigin = null,
    AuthenticationClientContext? ClientContext = null
) : ICommand<Result<AuthenticationModel>>;
