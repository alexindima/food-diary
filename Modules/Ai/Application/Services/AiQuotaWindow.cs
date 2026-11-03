using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Modules.Users.Contracts.Models;

namespace FoodDiary.Modules.Ai.Application.Services;

internal sealed record AiQuotaWindow(
    DateTime StartUtc, DateTime EndUtc, long InputLimit, long OutputLimit, bool IsTrial) {
    private const long TrialInputLimit = 500_000;
    private const long TrialOutputLimit = 100_000;

    public DateTime ReservationKeyUtc => IsTrial ? AiQuotaReservationRequest.TrialPeriodKeyUtc : StartUtc;

    public static bool HasExpiredTrialOnlyAccess(UserAiProfileModel profile, DateTime nowUtc) =>
        !profile.HasAssignedPremium && profile.PremiumTrialStartedAtUtc is not null &&
        !(profile.PremiumTrialStartedAtUtc <= nowUtc && profile.PremiumTrialEndsAtUtc > nowUtc);

    public static AiQuotaWindow Resolve(UserAiProfileModel profile, DateTime nowUtc) {
        if (!profile.HasAssignedPremium &&
            profile.PremiumTrialStartedAtUtc is { } startUtc && startUtc <= nowUtc &&
            profile.PremiumTrialEndsAtUtc is { } endUtc && endUtc > nowUtc) {
            return new AiQuotaWindow(startUtc, endUtc,
                Math.Min(profile.InputTokenLimit, TrialInputLimit), Math.Min(profile.OutputTokenLimit, TrialOutputLimit), IsTrial: true);
        }

        var monthStartUtc = new DateTime(nowUtc.Year, nowUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        return new AiQuotaWindow(monthStartUtc, monthStartUtc.AddMonths(1), profile.InputTokenLimit, profile.OutputTokenLimit, IsTrial: false);
    }
}
