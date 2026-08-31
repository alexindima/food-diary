namespace FoodDiary.Application.Admin.Models;

public sealed record AdminAiUsageDailyModel(
    DateOnly Date,
    int TotalTokens,
    int InputTokens,
    int OutputTokens);
