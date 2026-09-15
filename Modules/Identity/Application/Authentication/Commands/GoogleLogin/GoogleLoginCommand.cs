using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Models;
using FoodDiary.Modules.Identity.Application.Authentication.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.GoogleLogin;

public sealed record GoogleLoginCommand(
    string Credential,
    bool RememberMe = false,
    AuthenticationClientContext? ClientContext = null) : ICommand<Result<AuthenticationModel>>;
