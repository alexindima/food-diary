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
    DateTime ExpiresOnUtc,
    DateTime? TrialStartedAtUtc = null) {
    // A user can start only one trial. This reserved key cannot be a monthly boundary.
    public static DateTime TrialPeriodKeyUtc { get; } = DateTime.UnixEpoch.AddDays(1);
}
