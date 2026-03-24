using System.Security.Claims;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Presentation.Api.Extensions;

public static class UserExtensions {
    public static UserId? GetUserId(this ClaimsPrincipal user) {
        var userIdValue = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdValue, out var userGuid) && userGuid != Guid.Empty) {
            return new UserId(userGuid);
        }

        return null;
    }
}
