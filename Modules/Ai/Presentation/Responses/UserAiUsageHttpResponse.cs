namespace FoodDiary.Modules.Ai.Presentation.Responses;

public sealed record UserAiUsageHttpResponse(
    long InputLimit,
    long OutputLimit,
    long InputUsed,
    long OutputUsed,
    DateTime ResetAtUtc);
