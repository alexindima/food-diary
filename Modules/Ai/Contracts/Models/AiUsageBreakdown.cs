namespace FoodDiary.Modules.Ai.Contracts.Models;

public sealed record AiUsageBreakdown(
    string Key,
    int TotalTokens,
    int InputTokens,
    int OutputTokens);
