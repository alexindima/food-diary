namespace FoodDiary.Modules.Admin.Presentation.Requests;

public sealed record AdminUserUpdateHttpRequest(
    bool? IsActive,
    bool? IsEmailConfirmed,
    string[]? Roles,
    string? Language,
    long? AiInputTokenLimit,
    long? AiOutputTokenLimit
);
