using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Users.Models;

public sealed record UserAdminCreateModel(
    string Email,
    string? FirstName,
    string? LastName,
    string? Language,
    IReadOnlyCollection<string> Roles,
    string TemporaryPassword,
    bool IsEmailConfirmed,
    bool RequirePasswordChange,
    UserId ActorUserId,
    DateTime CreatedAtUtc);
