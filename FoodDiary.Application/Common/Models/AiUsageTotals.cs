namespace FoodDiary.Application.Common.Models;

public sealed record AiUsageTotals(
    long InputTokens,
    long OutputTokens);
