namespace FoodDiary.Modules.Ai.Contracts.Models;

public sealed record AiUsageDailySummary(
    DateOnly Date,
    int TotalTokens,
    int InputTokens,
    int OutputTokens);
