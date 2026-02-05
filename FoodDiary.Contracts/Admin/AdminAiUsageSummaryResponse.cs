namespace FoodDiary.Contracts.Admin;

public sealed record AdminAiUsageSummaryResponse(
    int TotalTokens,
    int InputTokens,
    int OutputTokens,
    IReadOnlyList<AdminAiUsageDailyResponse> ByDay,
    IReadOnlyList<AdminAiUsageBreakdownResponse> ByOperation,
    IReadOnlyList<AdminAiUsageBreakdownResponse> ByModel);
