namespace FoodDiary.Application.Abstractions.Authentication.Common;

public sealed record PasswordResetMessage(
    string ToEmail,
    string UserId,
    string Token,
    string? Language,
    string? ClientOrigin = null);
