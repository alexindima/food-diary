namespace FoodDiary.Modules.Admin.Application.Models;

public sealed record AdminAiUsageBreakdownModel(
    string Key,
    int TotalTokens,
    int InputTokens,
    int OutputTokens);
