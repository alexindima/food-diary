using System;

namespace FoodDiary.Contracts.Authentication;

public record VerifyEmailRequest(
    Guid UserId,
    string Token);

public record RequestPasswordResetRequest(
    string Email);

public record ConfirmPasswordResetRequest(
    Guid UserId,
    string Token,
    string NewPassword);
