namespace FoodDiary.Modules.Admin.Presentation.Responses;

public sealed record AdminAiUsageBreakdownHttpResponse(
    string Key,
    int TotalTokens,
    int InputTokens,
    int OutputTokens);
