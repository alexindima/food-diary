using System.Net;
using System.Text.Json;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.Enums;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class AdminUserPremiumGrantApiTests(CookieExpiryApiWebApplicationFactory factory) : IClassFixture<CookieExpiryApiWebApplicationFactory> {
    [RequiresDockerFact]
    public async Task AdministratorCanSetShortenAndRevokeManualPremiumSeparatelyFromAssignedAccess() {
        DateTimeOffset now = factory.Clock.UtcNow;
        using HttpClient administrator = CookieAuthenticationScenario.CreateBrowser(factory);
        await CookieAuthenticationScenario.CreateAdministratorAsync(factory, administrator);
        using HttpClient user = CookieAuthenticationScenario.CreateBrowser(factory);
        CookieIdentity target = await CookieAuthenticationScenario.RegisterAsync(factory, user);
        string path = $"/api/v1/admin/users/{target.Id}/premium-grant";
        DateTimeOffset originalEnd = now.AddHours(2).ToOffset(TimeSpan.FromHours(4));
        DateTimeOffset shortenedEnd = now.AddHours(1);

        using HttpResponseMessage forbidden = await user.PutAsJsonAsync(path, new { endsAtUtc = originalEnd, reason = "QA" });
        Assert.Equal(HttpStatusCode.Unauthorized, forbidden.StatusCode);
        using HttpResponseMessage blankReason = await administrator.PutAsJsonAsync(path, new { endsAtUtc = originalEnd, reason = " " });
        Assert.Equal(HttpStatusCode.BadRequest, blankReason.StatusCode);
        using HttpResponseMessage past = await administrator.PutAsJsonAsync(path, new { endsAtUtc = now, reason = "QA" });
        Assert.Equal(HttpStatusCode.BadRequest, past.StatusCode);
        using HttpResponseMessage granted = await administrator.PutAsJsonAsync(path, new { endsAtUtc = originalEnd, reason = "  QA grant  " });
        granted.EnsureSuccessStatusCode();
        JsonElement saved = await administrator.GetFromJsonAsync<JsonElement>(path);
        Assert.Equal(originalEnd.UtcDateTime, saved.GetProperty("endsAtUtc").GetDateTime());
        Assert.Equal("QA grant", saved.GetProperty("reason").GetString());

        using HttpResponseMessage shortened = await administrator.PutAsJsonAsync(path, new { endsAtUtc = shortenedEnd, reason = "QA expiry" });
        shortened.EnsureSuccessStatusCode();
        UserSessionPrincipalModel active = await ReadPrincipalAsync(target.Id, shortenedEnd.AddSeconds(-1));
        Assert.Contains(RoleNames.Premium, active.Roles, StringComparer.Ordinal);
        Assert.Equal(shortenedEnd.UtcDateTime, active.AccessTokenCapUtc);
        Assert.DoesNotContain(RoleNames.Premium, (await ReadPrincipalAsync(target.Id, shortenedEnd)).Roles, StringComparer.Ordinal);

        using HttpResponseMessage restored = await user.PostAsync("/api/v1/auth/session", content: null);
        restored.EnsureSuccessStatusCode();
        JsonElement overview = await user.GetFromJsonAsync<JsonElement>("/api/v1/billing/overview");
        Assert.True(overview.GetProperty("isPremium").GetBoolean());
        Assert.False(overview.GetProperty("renewalEnabled").GetBoolean());
        Assert.Equal(JsonValueKind.Null, overview.GetProperty("currentPeriodEndUtc").ValueKind);

        DateTimeOffset paidEnd = now.AddDays(7);
        await using (AsyncServiceScope scope = factory.Services.CreateAsyncScope()) {
            IUserRoleMembershipService roles = scope.ServiceProvider.GetRequiredService<IUserRoleMembershipService>();
            var userId = new UserId(target.Id);
            await roles.EnsureRoleAsync(userId, RoleNames.Premium);
            await roles.SetPremiumAccessEndAsync(userId, paidEnd.UtcDateTime);
        }

        using HttpResponseMessage revoked = await administrator.DeleteAsync(path);
        revoked.EnsureSuccessStatusCode();
        JsonElement cleared = await administrator.GetFromJsonAsync<JsonElement>(path);
        Assert.Equal(JsonValueKind.Null, cleared.GetProperty("endsAtUtc").ValueKind);
        Assert.Equal(JsonValueKind.Null, cleared.GetProperty("reason").ValueKind);
        UserSessionPrincipalModel paid = await ReadPrincipalAsync(target.Id, now);
        Assert.Contains(RoleNames.Premium, paid.Roles, StringComparer.Ordinal);
        Assert.Equal(paidEnd.UtcDateTime, paid.AccessTokenCapUtc);
    }

    private async Task<UserSessionPrincipalModel> ReadPrincipalAsync(Guid userId, DateTimeOffset at) {
        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();
        return (await scope.ServiceProvider.GetRequiredService<IUserSessionPrincipalReader>()
            .GetCurrentAsync(new UserId(userId), at.UtcDateTime)).Value;
    }
}
