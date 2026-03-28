namespace FoodDiary.Domain.ValueObjects;

public readonly record struct UserAdminAccountUpdate(
    bool? IsEmailConfirmed = null,
    string? Language = null,
    long? AiInputTokenLimit = null,
    long? AiOutputTokenLimit = null);
