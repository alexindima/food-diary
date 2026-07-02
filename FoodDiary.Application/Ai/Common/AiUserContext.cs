using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Ai.Common;

public sealed record AiUserContext(
    UserId UserId,
    string? Language,
    long InputTokenLimit,
    long OutputTokenLimit);
