using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Authentication.Models;
using FoodDiary.Application.Authentication.Models;

namespace FoodDiary.Application.Authentication.Commands.RestoreAccount;

public record RestoreAccountCommand(
    string Email,
    string Password,
    bool RememberMe = false,
    AuthenticationClientContext? ClientContext = null
) : ICommand<Result<AuthenticationModel>>;
