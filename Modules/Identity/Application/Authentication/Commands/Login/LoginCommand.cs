using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Identity.Application.Authentication.Models;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.Login;

public record LoginCommand(
    string Email,
    string Password,
    bool RememberMe = false,
    AuthenticationClientContext? ClientContext = null
) : ICommand<Result<AuthenticationModel>>;
