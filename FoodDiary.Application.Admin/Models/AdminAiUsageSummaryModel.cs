namespace FoodDiary.Application.Admin.Models;

public sealed record AdminAiUsageSummaryModel(
    int TotalTokens,
    int InputTokens,
    int OutputTokens,
    IReadOnlyList<AdminAiUsageDailyModel> ByDay,
    IReadOnlyList<AdminAiUsageBreakdownModel> ByOperation,
    IReadOnlyList<AdminAiUsageBreakdownModel> ByModel,
    IReadOnlyList<AdminAiUsageUserModel> ByUser);
