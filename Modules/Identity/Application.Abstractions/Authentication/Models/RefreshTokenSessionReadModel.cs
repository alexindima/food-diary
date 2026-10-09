using FoodDiary.Modules.Identity.Domain.ValueObjects.Ids;
namespace FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Models;

public sealed record RefreshTokenSessionReadModel(
    RefreshTokenSessionId Id,
    string? AuthProvider,
    string? UserAgent,
    DateTime CreatedAtUtc,
    DateTime LastRotatedAtUtc);
