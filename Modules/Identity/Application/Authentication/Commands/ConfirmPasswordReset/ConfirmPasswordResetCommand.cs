using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Identity.Application.Authentication.Models;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.ConfirmPasswordReset;

public record ConfirmPasswordResetCommand(
    Guid UserId,
    string Token,
    string NewPassword) : ICommand<Result<AuthenticationModel>>;
