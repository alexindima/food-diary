using System.Globalization;
using System.Security.Claims;
using System.Text.Encodings.Web;
using FoodDiary.Mediator;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Identity.Application.Authentication.Models;
using FoodDiary.Modules.Identity.Application.Authentication.Queries.AuthenticateBrowserSession;
using FoodDiary.Results;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using FoodDiary.Authentication.Contracts.Options;

namespace FoodDiary.Modules.Identity.Presentation.Security;

public sealed class BrowserSessionAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    ISender sender,
    BrowserSessionCookieService cookies,
    IOptions<JwtOptions> lifetime,
    TimeProvider clock) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder) {
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync() {
        string? credential = cookies.Read(Context);
        if (credential is null) {
            return AuthenticateResult.NoResult();
        }
        Result<BrowserSessionIdentityModel> result = await sender.Send(new AuthenticateBrowserSessionQuery(credential), Context.RequestAborted).ConfigureAwait(false);
        if (result.IsFailure) {
            return AuthenticateResult.Fail("The browser session is no longer valid.");
        }
        BrowserSessionIdentityModel session = result.Value;
        string expected = Request.Headers[BrowserSessionCookieService.SessionHeader].ToString();
        if (expected.Length > 0 && !string.Equals(expected, session.SessionId.ToString("D"), StringComparison.Ordinal)) {
            return AuthenticateResult.Fail("The request belongs to another browser session.");
        }
        List<Claim> claims = [
            new(ClaimTypes.NameIdentifier, session.Principal.UserId.Value.ToString("D")),
            new(JwtSecurityClaimNames.RefreshSessionId, session.SessionId.ToString("D")),
            new(JwtSecurityClaimNames.SecurityVersion, session.Principal.SecurityVersion.ToString(CultureInfo.InvariantCulture)),
        ];
        if (session.Principal.Email is { } email) {
            claims.Add(new Claim(ClaimTypes.Email, email));
        } else {
            claims.Add(new Claim("fd_email_optional", "true"));
        }
        claims.AddRange(session.Principal.Roles.Select(role => new Claim(ClaimTypes.Role, role)));
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name));
        DateTime connectionExpires = clock.GetUtcNow().UtcDateTime.AddMinutes(lifetime.Value.ExpirationMinutes);
        if (session.ExpiresAtUtc < connectionExpires) { connectionExpires = session.ExpiresAtUtc; }
        if (session.Principal.AccessTokenCapUtc is { } cap && cap < connectionExpires) { connectionExpires = cap; }
        return AuthenticateResult.Success(new AuthenticationTicket(principal,
            new AuthenticationProperties { ExpiresUtc = new DateTimeOffset(connectionExpires) }, Scheme.Name));
    }
}
