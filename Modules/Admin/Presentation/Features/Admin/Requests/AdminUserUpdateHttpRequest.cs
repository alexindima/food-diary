namespace FoodDiary.Presentation.Api.Features.Admin.Requests;

public sealed record AdminUserUpdateHttpRequest(
    bool? IsActive,
    bool? IsEmailConfirmed,
    string[]? Roles,
    string? Language,
    long? AiInputTokenLimit,
    long? AiOutputTokenLimit
);
