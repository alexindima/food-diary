namespace FoodDiary.Application.Abstractions.Authentication.Common;

public sealed record AccountCreatedMessage(
    string ToEmail,
    string TemporaryPassword,
    string? Language,
    string? ClientOrigin);
