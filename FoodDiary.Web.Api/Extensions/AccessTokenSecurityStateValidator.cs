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

        string? impersonationClaim = principal?.FindFirstValue(JwtImpersonationClaimNames.IsImpersonation);
        if (impersonationClaim is not null) {
            if (!string.Equals(impersonationClaim, "true", StringComparison.Ordinal) ||
                !Guid.TryParse(principal?.FindFirstValue(JwtImpersonationClaimNames.ActorUserId), out Guid actorId) ||
                actorId == Guid.Empty || actorId == userId ||
                !Guid.TryParse(principal?.FindFirstValue(JwtImpersonationClaimNames.ActorSessionId), out Guid actorSessionId) ||
                actorSessionId == Guid.Empty ||
                !long.TryParse(principal?.FindFirstValue(JwtImpersonationClaimNames.ActorSecurityVersion),
                    NumberStyles.None, CultureInfo.InvariantCulture, out long actorSecurityVersion)) {
                return false;
            }

            return await securityReader.IsCurrentAdministratorAsync(actorId, actorSecurityVersion, cancellationToken).ConfigureAwait(false) &&
                   await sessionReader.IsActiveAsync(actorId, actorSessionId, cancellationToken).ConfigureAwait(false);
        }

        return Guid.TryParse(principal?.FindFirstValue(JwtSecurityClaimNames.RefreshSessionId), out Guid sessionId) &&
               sessionId != Guid.Empty &&
               await sessionReader.IsActiveAsync(userId, sessionId, cancellationToken).ConfigureAwait(false);
    }
}
