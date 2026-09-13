namespace FoodDiary.Modules.Admin.Application.Models;

public sealed record AdminAiUsageUserModel(
    Guid Id,
    string? Email,
    int TotalTokens,
    int InputTokens,
    int OutputTokens);
