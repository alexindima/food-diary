using FoodDiary.Application.Abstractions.Authentication.Models;
using FoodDiary.Application.Identity.Authentication.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.Identity.Authentication.Commands.GoogleLogin;

public sealed record GoogleLoginCommand(
    string Credential,
    bool RememberMe = false,
    AuthenticationClientContext? ClientContext = null) : ICommand<Result<AuthenticationModel>>;
