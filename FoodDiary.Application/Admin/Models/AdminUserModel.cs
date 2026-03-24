namespace FoodDiary.Application.Admin.Models;

public sealed record AdminUserModel(
    Guid Id,
    string Email,
    string? Username,
    string? FirstName,
    string? LastName,
    string? Language,
    bool IsActive,
    bool IsEmailConfirmed,
    DateTime CreatedOnUtc,
    DateTime? DeletedAt,
    DateTime? LastLoginAtUtc,
    string[] Roles,
    long AiInputTokenLimit,
    long AiOutputTokenLimit);
