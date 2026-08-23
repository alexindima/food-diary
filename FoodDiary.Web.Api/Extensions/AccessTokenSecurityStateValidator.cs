using System.Globalization;
using System.Security.Claims;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Users.Common;

namespace FoodDiary.Web.Api.Extensions;

internal static class AccessTokenSecurityStateValidator {
    public static async Task<bool> IsCurrentAsync(
        ClaimsPrincipal? principal,
        IUserAccessTokenSecurityReader securityReader,
        CancellationToken cancellationToken) {
        string? userIdClaim = principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        string? securityVersionClaim = principal?.FindFirstValue(JwtSecurityClaimNames.SecurityVersion);
        if (!Guid.TryParse(userIdClaim, out Guid userId)) {
            return false;
        }

        long securityVersion = 0;
        if (!string.IsNullOrWhiteSpace(securityVersionClaim) &&
            !long.TryParse(
                securityVersionClaim,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out securityVersion)) {
            return false;
        }

        return await securityReader
            .IsCurrentAsync(userId, securityVersion, cancellationToken)
            .ConfigureAwait(false);
    }
}
