namespace FoodDiary.Application.Abstractions.Admin.Models;

public sealed record AiUsageDailySummary(
    DateOnly Date,
    int TotalTokens,
    int InputTokens,
    int OutputTokens);
