using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Users.Contracts.Models;

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
