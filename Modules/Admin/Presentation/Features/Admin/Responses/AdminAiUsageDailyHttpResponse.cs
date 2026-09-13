namespace FoodDiary.Modules.Admin.Presentation.Features.Admin.Responses;

public sealed record AdminAiUsageDailyHttpResponse(
    DateOnly Date,
    int TotalTokens,
    int InputTokens,
    int OutputTokens);
