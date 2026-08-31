namespace FoodDiary.Application.Abstractions.Ai.Common;

public sealed record AiUsageTotals(
    long InputTokens,
    long OutputTokens);
