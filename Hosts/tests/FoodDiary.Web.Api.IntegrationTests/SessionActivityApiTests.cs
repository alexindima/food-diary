using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Identity.Domain.Entities.Users;
using FoodDiary.Modules.Identity.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Identity.Presentation.Features.Auth.Requests;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class SessionActivityApiTests(CookieExpiryApiWebApplicationFactory factory) : IClassFixture<CookieExpiryApiWebApplicationFactory> {
    [RequiresDockerFact]
    public async Task OrdinaryRequests_RecordActivityAtFiveMinuteIntervalsWithoutExtendingSession() {
        CookieExpiryApiWebApplicationFactory.ControlledClock clock = factory.Clock;
        using HttpClient browser = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        using HttpResponseMessage registered = await browser.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterHttpRequest($"activity-{Guid.NewGuid():N}@example.com", "Password123!", "en"));
        registered.EnsureSuccessStatusCode();
        JsonElement payload = await registered.Content.ReadFromJsonAsync<JsonElement>();
        var id = new RefreshTokenSessionId(Guid.Parse(payload.GetProperty("sessionId").GetString()!));
        UserRefreshTokenSession original = await ReadSessionAsync(factory, id);
        DateTimeOffset started = clock.UtcNow;
        clock.UtcNow = started.AddMinutes(4);
        await RequestProfileAsync(browser);
        Assert.Equal(original.LastRotatedAtUtc, (await ReadSessionAsync(factory, id)).LastRotatedAtUtc);
        clock.UtcNow = started.AddMinutes(5);
        await RequestProfileAsync(browser);
        UserRefreshTokenSession active = await ReadSessionAsync(factory, id);
        Assert.True(active.LastRotatedAtUtc == clock.UtcNow.UtcDateTime,
            "Ordinary authenticated activity must update the session after five minutes without requiring reload.");
        clock.UtcNow = started.AddMinutes(9);
        await RequestProfileAsync(browser);
        Assert.Equal(active.LastRotatedAtUtc, (await ReadSessionAsync(factory, id)).LastRotatedAtUtc);
        clock.UtcNow = started.AddMinutes(10);
        await RequestProfileAsync(browser);
        UserRefreshTokenSession last = await ReadSessionAsync(factory, id);
        Assert.Equal(clock.UtcNow.UtcDateTime, last.LastRotatedAtUtc);
        Assert.Equal(original.ExpiresAtUtc, last.ExpiresAtUtc);
        Assert.Equal(original.RefreshTokenHash, last.RefreshTokenHash);
    }

    private static async Task RequestProfileAsync(HttpClient browser) {
        using HttpResponseMessage profile = await browser.GetAsync("/api/v1/users/info");
        Assert.Equal(HttpStatusCode.OK, profile.StatusCode);
    }

    private static async Task<UserRefreshTokenSession> ReadSessionAsync(WebApplicationFactory<Program> factory, RefreshTokenSessionId id) {
        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();
        UserRefreshTokenSession? session = await scope.ServiceProvider.GetRequiredService<IRefreshTokenSessionWriteRepository>().GetByIdAsync(id);
        return Assert.IsType<UserRefreshTokenSession>(session);
    }

}
