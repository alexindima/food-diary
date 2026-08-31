namespace FoodDiary.Application.Admin.Models;

public sealed record AdminAiUsageBreakdownModel(
    string Key,
    int TotalTokens,
    int InputTokens,
    int OutputTokens);
