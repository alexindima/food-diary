using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Admin.Models;

public sealed record AiUsageUserSummary(
    UserId UserId,
    string Email,
    int TotalTokens,
    int InputTokens,
    int OutputTokens);
