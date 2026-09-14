using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Ai.Contracts.Models;

public sealed record AiUsageUserSummary(
    UserId UserId,
    string? Email,
    int TotalTokens,
    int InputTokens,
    int OutputTokens);
