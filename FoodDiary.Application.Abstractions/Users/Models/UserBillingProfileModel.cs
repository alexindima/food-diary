using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Users.Models;

public sealed record UserBillingProfileModel(
    UserId UserId,
    string Email,
    bool IsActive,
    bool IsDeleted,
    bool HasPaidPremium,
    DateTime? PremiumTrialStartedAtUtc,
    DateTime? PremiumTrialEndsAtUtc);
