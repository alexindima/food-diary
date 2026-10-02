using FoodDiary.Modules.Identity.Contracts.Authentication.Common;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using System.Globalization;
using System.Security.Claims;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;

namespace FoodDiary.Web.Api.Extensions;

internal static class AccessTokenSecurityStateValidator {
    public static async Task<bool> IsCurrentAsync(
        ClaimsPrincipal? principal,
        IUserAccessTokenSecurityReader securityReader,
        IUserAccessTokenSessionReader sessionReader,
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

        if (!await securityReader.IsCurrentAsync(userId, securityVersion, cancellationToken).ConfigureAwait(false)) {
            return false;
        }

        if (string.Equals(principal?.FindFirstValue(JwtImpersonationClaimNames.IsImpersonation), "true", StringComparison.Ordinal)) {
            return Guid.TryParse(principal?.FindFirstValue(JwtImpersonationClaimNames.ActorUserId), out Guid actorId) &&
                   actorId != Guid.Empty;
        }

        return Guid.TryParse(principal?.FindFirstValue(JwtSecurityClaimNames.RefreshSessionId), out Guid sessionId) &&
               sessionId != Guid.Empty &&
               await sessionReader.IsActiveAsync(userId, sessionId, cancellationToken).ConfigureAwait(false);
    }
}
