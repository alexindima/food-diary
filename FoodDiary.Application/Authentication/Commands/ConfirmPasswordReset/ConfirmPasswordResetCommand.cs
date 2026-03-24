using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Authentication.Models;

namespace FoodDiary.Application.Authentication.Commands.ConfirmPasswordReset;

public record ConfirmPasswordResetCommand(
    Guid UserId,
    string Token,
    string NewPassword) : ICommand<Result<AuthenticationModel>>;
