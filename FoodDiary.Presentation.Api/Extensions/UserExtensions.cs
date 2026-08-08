using System.Security.Claims;

namespace FoodDiary.Presentation.Api.Extensions;

public static class UserExtensions {
    private static readonly string[] UserIdClaimTypes = [
        ClaimTypes.NameIdentifier,
        "nameid",
        "sub",
    ];

    extension(ClaimsPrincipal user) {
        public Guid? GetUserGuid() {
            string? userIdValue = UserIdClaimTypes
                .Select(user.FindFirstValue)
                .FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value));

            if (Guid.TryParse(userIdValue, out Guid userGuid) && userGuid != Guid.Empty) {
                return userGuid;
            }

            return null;
        }
    }
}
