namespace FoodDiary.Presentation.Api.Features.Admin.Responses;

public sealed record AdminAiUsageBreakdownHttpResponse(
    string Key,
    int TotalTokens,
    int InputTokens,
    int OutputTokens);
