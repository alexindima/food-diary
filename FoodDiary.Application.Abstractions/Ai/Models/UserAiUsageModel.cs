namespace FoodDiary.Application.Abstractions.Ai.Models;

public sealed record UserAiUsageModel(
    long InputLimit,
    long OutputLimit,
    long InputUsed,
    long OutputUsed,
    DateTime ResetAtUtc);
