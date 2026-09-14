namespace FoodDiary.Modules.Ai.Application.Abstractions.Models;

public sealed record UserAiUsageModel(
    long InputLimit,
    long OutputLimit,
    long InputUsed,
    long OutputUsed,
    DateTime ResetAtUtc);
