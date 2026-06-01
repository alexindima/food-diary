namespace FoodDiary.Application.Abstractions.Authentication.Common;

public sealed record EmailVerificationMessage(
    string ToEmail,
    string UserId,
    string Token,
    string? Language,
    string? ClientOrigin = null);
