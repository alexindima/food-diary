namespace FoodDiary.Application.Abstractions.Admin.Models;

public sealed record AiUsageSummary(
    int TotalTokens,
    int InputTokens,
    int OutputTokens,
    IReadOnlyList<AiUsageDailySummary> ByDay,
    IReadOnlyList<AiUsageBreakdown> ByOperation,
    IReadOnlyList<AiUsageBreakdown> ByModel,
    IReadOnlyList<AiUsageUserSummary> ByUser);
