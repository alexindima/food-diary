using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Identity.Presentation.Features.Auth.Requests;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.Enums;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class ImpersonationRevocationTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory> {
    [RequiresDockerFact]
    public async Task RemovingAdmin_ImmediatelyRejectsDelegationAndRestoringRoleDoesNotReviveIt() {
        using HttpClient actor = factory.CreateClient();
        using HttpClient target = factory.CreateClient();
        using HttpClient delegated = factory.CreateClient();
        (Guid actorId, string email) = await RegisterAsync(actor);
        (Guid targetId, _) = await RegisterAsync(target);
        await SetAdminAsync(actorId, enabled: true);
        await LoginAsync(actor, email);
        string code = await StartAsync(actor, targetId);
        await ExchangeAsync(delegated, code);
        await AssertReadAsync(delegated, HttpStatusCode.OK);

        // This is the real Users-owned persisted role-removal capability.
        await SetAdminAsync(actorId, enabled: false);

        await AssertReadAsync(delegated, HttpStatusCode.Unauthorized);
        await AssertReadAsync(target, HttpStatusCode.OK);
        await SetAdminAsync(actorId, enabled: true);
        await AssertReadAsync(delegated, HttpStatusCode.Unauthorized);
        await LoginAsync(actor, email);
        await ExchangeAsync(delegated, await StartAsync(actor, targetId));
        await AssertReadAsync(delegated, HttpStatusCode.OK);
    }

    [RequiresDockerTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RevokingOriginatingSession_RejectsDelegationButKeepsOtherActorSession(bool revokeAllOthers) {
        using HttpClient actor = factory.CreateClient();
        using HttpClient otherSession = factory.CreateClient();
        using HttpClient target = factory.CreateClient();
        using HttpClient delegated = factory.CreateClient();
        (Guid actorId, string email) = await RegisterAsync(actor);
        (Guid targetId, _) = await RegisterAsync(target);
        await SetAdminAsync(actorId, enabled: true);
        string actorToken = await LoginAsync(actor, email);
        await LoginAsync(otherSession, email);
        await ExchangeAsync(delegated, await StartAsync(actor, targetId));
        await AssertReadAsync(delegated, HttpStatusCode.OK);
        string sessionId = ClaimValue(actorToken, JwtSecurityClaimNames.RefreshSessionId);

        using HttpResponseMessage revoked = await otherSession.DeleteAsync(revokeAllOthers
            ? "/api/v1/auth/sessions"
            : $"/api/v1/auth/sessions/{sessionId}");

        Assert.Equal(HttpStatusCode.NoContent, revoked.StatusCode);
        await AssertReadAsync(delegated, HttpStatusCode.Unauthorized);
        await AssertReadAsync(otherSession, HttpStatusCode.OK);
        await AssertReadAsync(target, HttpStatusCode.OK);
        await ExchangeAsync(delegated, await StartAsync(otherSession, targetId));
        await AssertReadAsync(delegated, HttpStatusCode.OK);
    }

    [RequiresDockerFact]
    public async Task HandoffExchangedAfterAdminRevocation_CannotAuthorizeTargetReads() {
        using HttpClient actor = factory.CreateClient();
        using HttpClient target = factory.CreateClient();
        using HttpClient delegated = factory.CreateClient();
        (Guid actorId, string email) = await RegisterAsync(actor);
        (Guid targetId, _) = await RegisterAsync(target);
        await SetAdminAsync(actorId, enabled: true);
        await LoginAsync(actor, email);
        string code = await StartAsync(actor, targetId);

        await SetAdminAsync(actorId, enabled: false);
        await ExchangeAsync(delegated, code);

        await AssertReadAsync(delegated, HttpStatusCode.Unauthorized);
        await AssertReadAsync(target, HttpStatusCode.OK);
    }

    private async Task SetAdminAsync(Guid actorId, bool enabled) {
        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();
        IUserRoleMembershipService roles = scope.ServiceProvider.GetRequiredService<IUserRoleMembershipService>();
        if (enabled) {
            await roles.EnsureRoleAsync(new UserId(actorId), RoleNames.Admin);
        } else {
            await roles.RemoveRoleAsync(new UserId(actorId), RoleNames.Admin);
        }
    }

    private static async Task<(Guid Id, string Email)> RegisterAsync(HttpClient client) {
        string email = $"impersonation-revocation-{Guid.NewGuid():N}@example.com";
        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/auth/register", new RegisterHttpRequest(email, "Password123!", "en"));
        response.EnsureSuccessStatusCode();
        string token = await ReadAccessTokenAsync(response);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return (Guid.Parse(ClaimValue(token, ClaimTypes.NameIdentifier)), email);
    }

    private static async Task<string> LoginAsync(HttpClient client, string email) {
        client.DefaultRequestHeaders.Authorization = null;
        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/auth/login", new LoginHttpRequest(email, "Password123!"));
        response.EnsureSuccessStatusCode();
        string token = await ReadAccessTokenAsync(response);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return token;
    }

    private static async Task<string> StartAsync(HttpClient actor, Guid targetId) {
        using HttpResponseMessage response = await actor.PostAsJsonAsync(
            $"/api/v1/admin/users/{targetId}/impersonation", new { reason = "Investigating support ticket" });
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("code").GetString()!;
    }

    private static async Task ExchangeAsync(HttpClient client, string code) {
        client.DefaultRequestHeaders.Authorization = null;
        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/auth/impersonation/exchange", new ExchangeImpersonationHttpRequest(code));
        response.EnsureSuccessStatusCode();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await ReadAccessTokenAsync(response));
    }

    private static async Task AssertReadAsync(HttpClient client, HttpStatusCode expected) {
        using HttpResponseMessage response = await client.GetAsync("/api/v1/users/info");
        Assert.Equal(expected, response.StatusCode);
    }

    private static async Task<string> ReadAccessTokenAsync(HttpResponseMessage response) {
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.False(json.RootElement.TryGetProperty("refreshToken", out _));
        return json.RootElement.GetProperty("accessToken").GetString()!;
    }

    private static string ClaimValue(string token, string type) =>
        new JwtSecurityTokenHandler().ReadJwtToken(token).Claims.Single(claim => string.Equals(claim.Type, type, StringComparison.Ordinal)).Value;
}
