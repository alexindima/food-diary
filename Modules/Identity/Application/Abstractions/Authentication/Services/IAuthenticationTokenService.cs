using FoodDiary.Application.Abstractions.Authentication.Models;
using FoodDiary.Application.Abstractions.Users.Models;

namespace FoodDiary.Application.Abstractions.Authentication.Services;

public interface IAuthenticationTokenService {
    Task<IssuedAuthenticationTokens> IssueFromPrincipalAsync(
        UserAuthenticationPrincipalModel principal,
        CancellationToken cancellationToken,
        AuthenticationClientContext? clientContext = null,
        bool rememberMe = false);

    Task<IssuedAuthenticationTokens?> RotateFromPrincipalAsync(
        UserAuthenticationPrincipalModel principal,
        Guid refreshSessionId,
        string expectedRefreshTokenHash,
        bool rememberMe,
        CancellationToken cancellationToken);

}
