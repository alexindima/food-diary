using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Identity.Application.Authentication.Models;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Models;

namespace FoodDiary.Modules.Identity.Application.Authentication.Commands.ConfirmPasswordReset;

public record ConfirmPasswordResetCommand(
    Guid UserId,
    string Token,
    string NewPassword,
    AuthenticationClientContext? ClientContext = null) : ICommand<Result<AuthenticationModel>>;
