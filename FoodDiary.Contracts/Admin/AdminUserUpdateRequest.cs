namespace FoodDiary.Contracts.Admin;

public sealed record AdminUserUpdateRequest(
    bool? IsActive,
    bool? IsEmailConfirmed,
    string[] Roles,
    long? AiInputTokenLimit,
    long? AiOutputTokenLimit
);
