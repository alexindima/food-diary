using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Users.Models;

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
