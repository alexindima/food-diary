using System.Security.Claims;
using System.Text.Encodings.Web;
using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

public sealed class TestAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder) {
    public const string SchemeName = "Test";
    public const string AuthenticateHeader = "X-Test-Auth";
    public const string UserIdHeader = "X-Test-UserId";
    public const string RoleHeader = "X-Test-Role";
    public const string ImpersonationHeader = "X-Test-Impersonation";
    public const string ImpersonationActorUserIdHeader = "X-Test-Impersonation-ActorUserId";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync() {
        if (!Request.Headers.TryGetValue(AuthenticateHeader, out var authenticate) ||
            !string.Equals(authenticate.ToString(), "true", StringComparison.OrdinalIgnoreCase)) {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new List<Claim>();

        if (Request.Headers.TryGetValue(UserIdHeader, out var userId) &&
            !string.IsNullOrWhiteSpace(userId.ToString())) {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId.ToString()));
        }

        if (Request.Headers.TryGetValue(RoleHeader, out var role) &&
            !string.IsNullOrWhiteSpace(role.ToString())) {
            claims.Add(new Claim(ClaimTypes.Role, role.ToString()));
        }

        if (Request.Headers.TryGetValue(ImpersonationHeader, out var impersonation) &&
            string.Equals(impersonation.ToString(), "true", StringComparison.OrdinalIgnoreCase)) {
            claims.Add(new Claim(JwtImpersonationClaimNames.IsImpersonation, "true"));

            if (Request.Headers.TryGetValue(ImpersonationActorUserIdHeader, out var actorUserId) &&
                !string.IsNullOrWhiteSpace(actorUserId.ToString())) {
                claims.Add(new Claim(JwtImpersonationClaimNames.ActorUserId, actorUserId.ToString()));
            }
        }

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
