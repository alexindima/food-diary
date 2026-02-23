using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Contracts.Authentication;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Authentication.Commands.ConfirmPasswordReset;

public record ConfirmPasswordResetCommand(
    UserId UserId,
    string Token,
    string NewPassword) : ICommand<Result<AuthenticationResponse>>;
