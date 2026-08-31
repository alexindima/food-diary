using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Ai.Common;

public sealed record AiQuotaReservationRequest(
    string RequestId,
    UserId UserId,
    DateTime PeriodStartUtc,
    string Operation,
    long InputTokens,
    long OutputTokens,
    long InputTokenLimit,
    long OutputTokenLimit,
    DateTime ExpiresOnUtc);
