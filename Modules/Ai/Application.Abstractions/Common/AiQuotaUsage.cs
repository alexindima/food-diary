namespace FoodDiary.Modules.Ai.Application.Abstractions.Common;

public sealed record AiQuotaUsage(
    string Operation,
    string Model,
    int InputTokens,
    int OutputTokens,
    int TotalTokens);
