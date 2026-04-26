using FoodDiary.Application.Authentication.Models;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;

namespace FoodDiary.Application.Authentication.Commands.GoogleLogin;

public sealed record GoogleLoginCommand(string Credential) : ICommand<Result<AuthenticationModel>>;
