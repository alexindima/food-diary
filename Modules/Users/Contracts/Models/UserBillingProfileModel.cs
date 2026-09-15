using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Users.Contracts.Models;

public sealed record UserBillingProfileModel(
    UserId UserId,
    string? Email,
    bool IsActive,
    bool IsDeleted,
    bool HasPaidPremium,
    DateTime? PremiumTrialStartedAtUtc,
    DateTime? PremiumTrialEndsAtUtc,
    bool IsEmailConfirmed = false);
