using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Authentication.Models;

namespace FoodDiary.Application.Authentication.Commands.Login;

public record LoginCommand(
    string Email,
    string Password
) : ICommand<Result<AuthenticationModel>>;
