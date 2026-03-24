namespace FoodDiary.Presentation.Api.Features.Ai.Responses;

public sealed record UserAiUsageHttpResponse(
    long InputLimit,
    long OutputLimit,
    long InputUsed,
    long OutputUsed,
    DateTime ResetAtUtc);
