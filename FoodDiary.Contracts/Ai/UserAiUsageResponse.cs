namespace FoodDiary.Contracts.Ai;

public sealed record UserAiUsageResponse(
    long InputLimit,
    long OutputLimit,
    long InputUsed,
    long OutputUsed,
    DateTime ResetAtUtc);
