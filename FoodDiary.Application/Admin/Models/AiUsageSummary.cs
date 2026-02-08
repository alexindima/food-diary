using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Admin.Models;

public sealed record AiUsageSummary(
    int TotalTokens,
    int InputTokens,
    int OutputTokens,
    IReadOnlyList<AiUsageDailySummary> ByDay,
    IReadOnlyList<AiUsageBreakdown> ByOperation,
    IReadOnlyList<AiUsageBreakdown> ByModel,
    IReadOnlyList<AiUsageUserSummary> ByUser);

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

public sealed record AiUsageUserSummary(
    UserId UserId,
    string Email,
    int TotalTokens,
    int InputTokens,
    int OutputTokens);
