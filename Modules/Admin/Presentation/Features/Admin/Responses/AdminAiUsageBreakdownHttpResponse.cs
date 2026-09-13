namespace FoodDiary.Modules.Admin.Presentation.Features.Admin.Responses;

public sealed record AdminAiUsageBreakdownHttpResponse(
    string Key,
    int TotalTokens,
    int InputTokens,
    int OutputTokens);
