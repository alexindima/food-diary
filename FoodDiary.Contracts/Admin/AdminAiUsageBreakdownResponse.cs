namespace FoodDiary.Contracts.Admin;

public sealed record AdminAiUsageBreakdownResponse(
    string Key,
    int TotalTokens,
    int InputTokens,
    int OutputTokens);
