namespace FoodDiary.Modules.Admin.Application.Models;

public sealed record AdminAiUsageDailyModel(
    DateOnly Date,
    int TotalTokens,
    int InputTokens,
    int OutputTokens);
