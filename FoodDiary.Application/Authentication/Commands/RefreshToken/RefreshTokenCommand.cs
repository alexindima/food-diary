using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;

namespace FoodDiary.Application.Authentication.Commands.RefreshToken;

public record RefreshTokenCommand(
    string RefreshToken
) : ICommand<Result<string>>;
