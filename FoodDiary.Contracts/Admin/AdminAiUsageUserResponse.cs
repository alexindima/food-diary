namespace FoodDiary.Contracts.Admin;

public sealed record AdminAiUsageUserResponse(
    Guid Id,
    string Email,
    int TotalTokens,
    int InputTokens,
    int OutputTokens);
