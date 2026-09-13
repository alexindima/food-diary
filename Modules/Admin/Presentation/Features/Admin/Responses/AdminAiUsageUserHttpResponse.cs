namespace FoodDiary.Modules.Admin.Presentation.Features.Admin.Responses;

public sealed record AdminAiUsageUserHttpResponse(
    Guid Id,
    string? Email,
    int TotalTokens,
    int InputTokens,
    int OutputTokens);
