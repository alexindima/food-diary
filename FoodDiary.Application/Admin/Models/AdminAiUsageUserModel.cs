namespace FoodDiary.Application.Admin.Models;

public sealed record AdminAiUsageUserModel(
    Guid Id,
    string Email,
    int TotalTokens,
    int InputTokens,
    int OutputTokens);
