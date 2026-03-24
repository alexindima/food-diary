namespace FoodDiary.Presentation.Api.Features.Admin.Responses;

public sealed record AdminAiUsageUserHttpResponse(
    Guid Id,
    string Email,
    int TotalTokens,
    int InputTokens,
    int OutputTokens);
