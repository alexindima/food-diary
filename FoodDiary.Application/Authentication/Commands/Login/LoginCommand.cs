using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Authentication.Models;
using FoodDiary.Application.Authentication.Models;

namespace FoodDiary.Application.Authentication.Commands.Login;

public record LoginCommand(
    string Email,
    string Password,
    AuthenticationClientContext? ClientContext = null
) : ICommand<Result<AuthenticationModel>>;
