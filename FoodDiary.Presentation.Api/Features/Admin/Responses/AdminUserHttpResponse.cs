namespace FoodDiary.Presentation.Api.Features.Admin.Responses;

public sealed record AdminUserHttpResponse(
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
