using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Ai.Application.Abstractions.Common;

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
