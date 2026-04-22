using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Billing.Services;

public sealed class BillingAccessService(
    IUserRepository userRepository,
    IDateTimeProvider dateTimeProvider) {
    public async Task EnsurePremiumRoleAsync(User user, bool shouldHavePremium, CancellationToken cancellationToken) {
        var currentRoles = user.GetRoleNames().ToList();
        var hasPremium = currentRoles.Contains(RoleNames.Premium, StringComparer.Ordinal);
        if (hasPremium == shouldHavePremium) {
            return;
        }

        if (shouldHavePremium) {
            currentRoles.Add(RoleNames.Premium);
        } else {
            currentRoles.RemoveAll(role => string.Equals(role, RoleNames.Premium, StringComparison.Ordinal));
        }

        var roleEntities = await userRepository.GetRolesByNamesAsync(currentRoles, cancellationToken);
        user.ReplaceRoles(roleEntities);
        await userRepository.UpdateAsync(user, cancellationToken);
    }

    public bool ShouldHavePremiumAccess(string status, DateTime? currentPeriodEndUtc) {
        if (string.IsNullOrWhiteSpace(status)) {
            return false;
        }

        return status.Trim().ToLowerInvariant() switch {
            "trialing" => true,
            "active" => true,
            "past_due" => !currentPeriodEndUtc.HasValue || currentPeriodEndUtc > dateTimeProvider.UtcNow,
            _ => false,
        };
    }
}
