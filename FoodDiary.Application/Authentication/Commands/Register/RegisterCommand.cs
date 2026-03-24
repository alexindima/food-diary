using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Authentication.Models;

namespace FoodDiary.Application.Authentication.Commands.Register;

public record RegisterCommand(
    string Email,
    string Password,
    string? Language
) : ICommand<Result<AuthenticationModel>>;
