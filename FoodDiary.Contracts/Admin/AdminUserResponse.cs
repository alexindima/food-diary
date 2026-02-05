namespace FoodDiary.Contracts.Admin;

public sealed record AdminUserResponse(
    Guid Id,
    string Email,
    string? Username,
    string? FirstName,
    string? LastName,
    bool IsActive,
    DateTime CreatedOnUtc,
    DateTime? DeletedAt,
    string[] Roles
);
