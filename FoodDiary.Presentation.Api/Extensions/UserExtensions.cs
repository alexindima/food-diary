using System.Security.Claims;

namespace FoodDiary.Presentation.Api.Extensions;

public static class UserExtensions {
    public static Guid? GetUserGuid(this ClaimsPrincipal user) {
        var userIdValue = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdValue, out var userGuid) && userGuid != Guid.Empty) {
            return userGuid;
        }

        return null;
    }
}
