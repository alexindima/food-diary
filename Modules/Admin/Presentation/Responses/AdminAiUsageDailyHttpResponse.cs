namespace FoodDiary.Modules.Admin.Presentation.Responses;

public sealed record AdminAiUsageDailyHttpResponse(
    DateOnly Date,
    int TotalTokens,
    int InputTokens,
    int OutputTokens);
