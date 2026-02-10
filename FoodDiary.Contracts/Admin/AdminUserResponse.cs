namespace FoodDiary.Contracts.Admin;

public sealed record AdminUserResponse(
    Guid Id,
    string Email,
    string? Username,
    string? FirstName,
    string? LastName,
    bool IsActive,
    bool IsEmailConfirmed,
    DateTime CreatedOnUtc,
    DateTime? DeletedAt,
    DateTime? LastLoginAtUtc,
    string[] Roles,
    long AiInputTokenLimit,
    long AiOutputTokenLimit
);
