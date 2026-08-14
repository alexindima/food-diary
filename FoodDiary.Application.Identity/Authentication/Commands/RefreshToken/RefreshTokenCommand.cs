using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Identity.Authentication.Models;

namespace FoodDiary.Application.Identity.Authentication.Commands.RefreshToken;

public record RefreshTokenCommand(
    string RefreshToken
) : ICommand<Result<AuthenticationModel>>;
