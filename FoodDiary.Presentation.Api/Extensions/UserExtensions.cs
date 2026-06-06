using System.Security.Claims;

namespace FoodDiary.Presentation.Api.Extensions;

public static class UserExtensions {
    private static readonly string[] UserIdClaimTypes = [
        ClaimTypes.NameIdentifier,
        "nameid",
        "sub",
    ];

    public static Guid? GetUserGuid(this ClaimsPrincipal user) {
        string? userIdValue = UserIdClaimTypes
            .Select(user.FindFirstValue)
            .FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value));

        if (Guid.TryParse(userIdValue, out Guid userGuid) && userGuid != Guid.Empty) {
            return userGuid;
        }

        return null;
    }
}
