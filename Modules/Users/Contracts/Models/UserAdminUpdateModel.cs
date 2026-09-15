using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Users.Contracts.Models;

public sealed record UserAdminUpdateModel(
    UserId UserId,
    bool? IsActive,
    bool? IsEmailConfirmed,
    IReadOnlyCollection<string>? Roles,
    string? Language,
    long? AiInputTokenLimit,
    long? AiOutputTokenLimit,
    UserId? ActorUserId,
    DateTime UpdatedAtUtc);
