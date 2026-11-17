using System.Globalization;
using System.Security.Claims;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Identity.Contracts.Authentication.Common;
using FoodDiary.Modules.Users.Contracts.Common;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace FoodDiary.Modules.Identity.Presentation.Security;

internal sealed class DelegatedAccessTokenValidator(
    IUserAccessTokenSecurityReader security,
    IUserAccessTokenSessionReader sessions) : IDelegatedAccessTokenValidator {
    public async Task<bool> IsCurrentAsync(ClaimsPrincipal principal, CancellationToken cancellationToken) =>
        Guid.TryParse(principal.GetClaim(Claims.Subject), out Guid userId) && userId != Guid.Empty &&
        Guid.TryParse(principal.GetClaim(JwtSecurityClaimNames.RefreshSessionId), out Guid sessionId) && sessionId != Guid.Empty &&
        long.TryParse(principal.GetClaim(JwtSecurityClaimNames.SecurityVersion), NumberStyles.None,
            CultureInfo.InvariantCulture, out long securityVersion) &&
        await security.IsCurrentAsync(userId, securityVersion, cancellationToken) &&
        await sessions.IsActiveAsync(userId, sessionId, cancellationToken);
}
