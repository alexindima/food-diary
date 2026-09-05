namespace FoodDiary.Presentation.Api.Features.Admin.Responses;

public sealed record AdminAiUsageSummaryHttpResponse(
    int TotalTokens,
    int InputTokens,
    int OutputTokens,
    IReadOnlyList<AdminAiUsageDailyHttpResponse> ByDay,
    IReadOnlyList<AdminAiUsageBreakdownHttpResponse> ByOperation,
    IReadOnlyList<AdminAiUsageBreakdownHttpResponse> ByModel,
    IReadOnlyList<AdminAiUsageUserHttpResponse> ByUser);
