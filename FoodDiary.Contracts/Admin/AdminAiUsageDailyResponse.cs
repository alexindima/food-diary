namespace FoodDiary.Contracts.Admin;

public sealed record AdminAiUsageDailyResponse(
    DateOnly Date,
    int TotalTokens,
    int InputTokens,
    int OutputTokens);
