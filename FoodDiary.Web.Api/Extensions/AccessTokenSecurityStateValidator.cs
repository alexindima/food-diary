using FoodDiary.Modules.Identity.Contracts.Authentication.Common;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using System.Globalization;
using System.Security.Claims;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;

namespace FoodDiary.Web.Api.Extensions;

internal static class AccessTokenSecurityStateValidator {
    public static Task<bool> IsCurrentAsync(
        ClaimsPrincipal? principal,
        IUserAccessTokenSecurityReader securityReader,
        IUserAccessTokenSessionReader sessionReader,
        CancellationToken cancellationToken) =>
        IsCurrentAsync(principal, securityReader, sessionReader, allowRequiredPasswordChange: false, cancellationToken);

    public static async Task<bool> IsCurrentAsync(
        ClaimsPrincipal? principal,
        IUserAccessTokenSecurityReader securityReader,
        IUserAccessTokenSessionReader sessionReader,
        bool allowRequiredPasswordChange,
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

        bool isCurrent = allowRequiredPasswordChange
            ? await securityReader.IsCurrentForPasswordChangeAsync(userId, securityVersion, cancellationToken)
            : await securityReader.IsCurrentAsync(userId, securityVersion, cancellationToken);
        if (!isCurrent) {
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

            return await securityReader.IsCurrentAdministratorAsync(actorId, actorSecurityVersion, cancellationToken) &&
                   await sessionReader.IsActiveAsync(actorId, actorSessionId, cancellationToken);
        }

        return Guid.TryParse(principal?.FindFirstValue(JwtSecurityClaimNames.RefreshSessionId), out Guid sessionId) &&
               sessionId != Guid.Empty &&
               await sessionReader.IsActiveAsync(userId, sessionId, cancellationToken);
    }
}
