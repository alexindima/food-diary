namespace FoodDiary.Application.Abstractions.Admin.Models;

public sealed record AiUsageBreakdown(
    string Key,
    int TotalTokens,
    int InputTokens,
    int OutputTokens);
