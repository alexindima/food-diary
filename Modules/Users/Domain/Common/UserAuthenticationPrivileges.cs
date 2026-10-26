using FoodDiary.Modules.Users.Domain.Contracts.Enums;

namespace FoodDiary.Modules.Users.Domain.Common;

public static class UserAuthenticationPrivileges {
    public static (IReadOnlyCollection<string> Roles, DateTime? AccessTokenCapUtc) Evaluate(
        IEnumerable<string> assignedRoles, DateTime? trialStartedAtUtc, DateTime? trialEndsAtUtc, DateTime nowUtc) {
        if (nowUtc.Kind == DateTimeKind.Unspecified) {
            throw new ArgumentOutOfRangeException(nameof(nowUtc), "UTC timestamp kind must be specified.");
        }
        DateTime now = nowUtc.ToUniversalTime();
        List<string> roles = [.. assignedRoles.Where(name => !string.IsNullOrWhiteSpace(name)).Distinct(StringComparer.Ordinal)];
        bool paidPremium = roles.Contains(RoleNames.Premium, StringComparer.Ordinal);
        bool activeTrial = trialStartedAtUtc <= now && trialEndsAtUtc > now;
        if (activeTrial && !paidPremium) {
            roles.Add(RoleNames.Premium);
        }
        return (roles, activeTrial && !paidPremium ? trialEndsAtUtc : null);
    }
}
