namespace FoodDiary.Application.Common.Models;

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
