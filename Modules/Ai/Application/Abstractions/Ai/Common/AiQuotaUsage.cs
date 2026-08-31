namespace FoodDiary.Application.Abstractions.Ai.Common;

public sealed record AiQuotaUsage(
    string Operation,
    string Model,
    int InputTokens,
    int OutputTokens,
    int TotalTokens);
