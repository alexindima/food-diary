using FoodDiary.Application.Abstractions.Authentication.Models;
using FoodDiary.Application.Authentication.Models;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;

namespace FoodDiary.Application.Authentication.Commands.GoogleLogin;

public sealed record GoogleLoginCommand(
    string Credential,
    AuthenticationClientContext? ClientContext = null) : ICommand<Result<AuthenticationModel>>;
