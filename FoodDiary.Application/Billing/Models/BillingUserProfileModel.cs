namespace FoodDiary.Application.Billing.Models;

public sealed record BillingUserProfileModel(
    bool HasPaidPremium,
    DateTime? PremiumTrialStartedAtUtc,
    DateTime? PremiumTrialEndsAtUtc) {
    public bool HasActivePremiumTrial(DateTime nowUtc) =>
        PremiumTrialStartedAtUtc.HasValue &&
        PremiumTrialEndsAtUtc.HasValue &&
        PremiumTrialStartedAtUtc.Value <= nowUtc &&
        PremiumTrialEndsAtUtc.Value > nowUtc;

    public bool HasUsedPremiumTrial() => PremiumTrialStartedAtUtc.HasValue || PremiumTrialEndsAtUtc.HasValue;
}
