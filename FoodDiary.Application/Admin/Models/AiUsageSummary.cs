namespace FoodDiary.Application.Admin.Models;

public sealed record AiUsageSummary(
    int TotalTokens,
    int InputTokens,
    int OutputTokens,
    IReadOnlyList<AiUsageDailySummary> ByDay,
    IReadOnlyList<AiUsageBreakdown> ByOperation,
    IReadOnlyList<AiUsageBreakdown> ByModel);

public sealed record AiUsageDailySummary(
    DateOnly Date,
    int TotalTokens,
    int InputTokens,
    int OutputTokens);

public sealed record AiUsageBreakdown(
    string Key,
    int TotalTokens,
    int InputTokens,
    int OutputTokens);
