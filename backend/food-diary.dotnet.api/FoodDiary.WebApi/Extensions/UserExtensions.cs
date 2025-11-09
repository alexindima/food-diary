using System;
using System.Security.Claims;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.WebApi.Extensions;

public static class UserExtensions
{
    public static UserId? GetUserId(this ClaimsPrincipal user)
    {
        var userIdValue = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdValue, out var userGuid))
        {
            return new UserId(userGuid);
        }

        return null;
    }
}
