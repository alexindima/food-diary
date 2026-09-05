namespace FoodDiary.Application.Abstractions.Authentication.Models;

public sealed record RefreshTokenSessionReadModel(
    Guid Id,
    string? AuthProvider,
    string? UserAgent,
    DateTime CreatedAtUtc,
    DateTime LastRotatedAtUtc);
