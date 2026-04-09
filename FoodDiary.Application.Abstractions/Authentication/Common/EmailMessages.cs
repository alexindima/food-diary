namespace FoodDiary.Application.Authentication.Common;

public sealed record EmailVerificationMessage(
    string ToEmail,
    string UserId,
    string Token,
    string? Language);

public sealed record PasswordResetMessage(
    string ToEmail,
    string UserId,
    string Token,
    string? Language);
