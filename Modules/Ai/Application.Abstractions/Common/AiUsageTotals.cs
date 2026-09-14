namespace FoodDiary.Modules.Ai.Application.Abstractions.Common;

public sealed record AiUsageTotals(
    long InputTokens,
    long OutputTokens);
