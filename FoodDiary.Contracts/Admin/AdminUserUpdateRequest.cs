namespace FoodDiary.Contracts.Admin;

public sealed record AdminUserUpdateRequest(
    bool? IsActive,
    bool? IsEmailConfirmed,
    string[] Roles,
    string? Language,
    long? AiInputTokenLimit,
    long? AiOutputTokenLimit
);
