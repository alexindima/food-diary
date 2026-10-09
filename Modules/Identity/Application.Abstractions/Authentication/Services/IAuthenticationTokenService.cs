using FoodDiary.Modules.Identity.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Models;
using FoodDiary.Modules.Users.Contracts.Models;

namespace FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Services;

public interface IAuthenticationTokenService {
    Task<IssuedAuthenticationTokens> IssueFromPrincipalAsync(
        UserAuthenticationPrincipalModel principal,
        CancellationToken cancellationToken,
        AuthenticationClientContext? clientContext = null,
        bool rememberMe = false);

    Task<IssuedAuthenticationTokens?> RotateFromPrincipalAsync(
        UserAuthenticationPrincipalModel principal,
        RefreshTokenSessionId refreshSessionId,
        string expectedRefreshTokenHash,
        bool rememberMe,
        CancellationToken cancellationToken);

}
