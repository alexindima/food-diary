namespace FoodDiary.Application.Abstractions.Authentication.Common;

public sealed record EmailVerificationMessage(
    string ToEmail,
    string UserId,
    string Token,
    string? Language,
    string? ClientOrigin = null);

public sealed record PasswordResetMessage(
    string ToEmail,
    string UserId,
    string Token,
    string? Language,
    string? ClientOrigin = null);

public sealed record TestEmailMessage(
    string ToEmail,
    string? Language);
