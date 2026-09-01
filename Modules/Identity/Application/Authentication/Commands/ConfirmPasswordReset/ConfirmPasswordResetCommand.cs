using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Identity.Authentication.Models;

namespace FoodDiary.Application.Identity.Authentication.Commands.ConfirmPasswordReset;

public record ConfirmPasswordResetCommand(
    Guid UserId,
    string Token,
    string NewPassword) : ICommand<Result<AuthenticationModel>>;
