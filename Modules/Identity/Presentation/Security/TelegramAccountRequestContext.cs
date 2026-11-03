using System.Globalization;
using System.Security.Claims;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using Microsoft.AspNetCore.Http;

namespace FoodDiary.Modules.Identity.Presentation.Security;

public static class TelegramAccountRequestContext {
    public static long? ReadSecurityVersion(HttpContext context) {
        Claim[] claims = [.. context.User.FindAll(JwtSecurityClaimNames.SecurityVersion).Take(2)];
        return claims.Length == 1 && long.TryParse(claims[0].Value, NumberStyles.None, CultureInfo.InvariantCulture, out long version)
            && version >= 0 ? version : null;
    }
}
