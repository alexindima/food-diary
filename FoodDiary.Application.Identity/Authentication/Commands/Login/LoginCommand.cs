using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Authentication.Models;
using FoodDiary.Application.Identity.Authentication.Models;

namespace FoodDiary.Application.Identity.Authentication.Commands.Login;

public record LoginCommand(
    string Email,
    string Password,
    bool RememberMe = false,
    AuthenticationClientContext? ClientContext = null
) : ICommand<Result<AuthenticationModel>>;
