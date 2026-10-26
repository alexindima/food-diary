using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Docker.DotNet.Models;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.Redis;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class SessionReplicaPersistenceApiTests(PostgresApiWebApplicationFactory database)
    : IClassFixture<PostgresApiWebApplicationFactory> {
    [RequiresDockerFact]
    public async Task OrdinarySession_SurvivesHostRestartAndDifferentKeyRing_AndRevocationReachesOtherReplica() {
        CookieIdentity identity;
        await using (WebApplicationFactory<Program> first = Replica(new Dictionary<string, string?>(StringComparer.Ordinal) {
            ["DataProtection:ApplicationName"] = "ordinary-first-" + Guid.NewGuid().ToString("N"),
        })) {
            using HttpClient browser = first.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
            identity = await CookieAuthenticationScenario.RegisterAsync(database, browser);
        }

        // Independent DI containers share only PostgreSQL. Ordinary credentials do not need Data Protection or Redis.
        await using WebApplicationFactory<Program> second = Replica(new Dictionary<string, string?>(StringComparer.Ordinal) {
            ["DataProtection:ApplicationName"] = "ordinary-second-" + Guid.NewGuid().ToString("N"),
        });
        await using WebApplicationFactory<Program> third = Replica(new Dictionary<string, string?>(StringComparer.Ordinal) {
            ["DataProtection:ApplicationName"] = "ordinary-third-" + Guid.NewGuid().ToString("N"),
        });
        using HttpClient restored = Browser(second, identity.Cookie);
        using HttpClient otherReplica = Browser(third, identity.Cookie);
        await CookieAuthenticationScenario.AssertIdentityAsync(restored, identity.Id);
        await CookieAuthenticationScenario.AssertIdentityAsync(otherReplica, identity.Id);
        using HttpResponseMessage logout = await restored.PostAsJsonAsync("/api/v1/auth/logout", new { });
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        await CookieAuthenticationScenario.AssertRejectedSessionAsync(otherReplica);
    }

    [RequiresDockerFact]
    public async Task ProtectedSupportContextAndOneTimeSso_SurviveHostRestartWithSharedRedisAndKeyRing() {
        await using RedisContainer redis = new RedisBuilder("redis:7-alpine@sha256:e7723ff73d963f5cc6d9c4643ea3d989527a402a319239054e9472a7fb9219a2")
            .WithCreateParameterModifier(parameters => {
                parameters.HostConfig ??= new HostConfig();
                parameters.HostConfig.PortBindings ??= new Dictionary<string, IList<PortBinding>>(StringComparer.Ordinal);
                parameters.HostConfig.PortBindings["6379/tcp"] = [new PortBinding { HostIP = IPAddress.Loopback.ToString(), HostPort = "0" }];
            }).Build();
        await redis.StartAsync();
        string keyRing = Path.Combine(AppContext.BaseDirectory, "session-replica-keys", Guid.NewGuid().ToString("N"));
        var settings = new Dictionary<string, string?>(StringComparer.Ordinal) {
            ["ConnectionStrings:Redis"] = redis.GetConnectionString(),
            ["DataProtection:KeyRingPath"] = keyRing,
            ["DataProtection:ApplicationName"] = "session-replicas-" + Guid.NewGuid().ToString("N"),
        };
        CookieIdentity target;
        string contextCookie;
        string contextId;
        string ssoCode;
        await using (WebApplicationFactory<Program> first = Replica(settings)) {
            using HttpClient administrator = first.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
            using HttpClient targetBrowser = first.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
            await CookieAuthenticationScenario.CreateAdministratorAsync(database, administrator);
            target = await CookieAuthenticationScenario.RegisterAsync(database, targetBrowser);
            using HttpResponseMessage start = await administrator.PostAsJsonAsync($"/api/v1/admin/users/{target.Id}/impersonation",
                new { reason = "Testing shared support-session persistence" });
            Assert.Equal(HttpStatusCode.OK, start.StatusCode);
            JsonElement handoff = await start.Content.ReadFromJsonAsync<JsonElement>();
            using HttpResponseMessage exchange = await administrator.PostAsJsonAsync("/api/v1/auth/impersonation/exchange",
                new { code = handoff.GetProperty("code").GetString() });
            Assert.Equal(HttpStatusCode.OK, exchange.StatusCode);
            contextId = (await exchange.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("contextId").GetString()!;
            contextCookie = Assert.Single(exchange.Headers.GetValues("Set-Cookie")).Split(';')[0];
            using HttpResponseMessage sso = await administrator.PostAsync("/api/v1/auth/admin-sso/start", content: null);
            Assert.Equal(HttpStatusCode.OK, sso.StatusCode);
            ssoCode = (await sso.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString()!;
        }
        Assert.NotEmpty(Directory.EnumerateFiles(keyRing, "key-*.xml"));

        await using WebApplicationFactory<Program> restarted = Replica(settings);
        await using WebApplicationFactory<Program> concurrent = Replica(settings);
        Assert.Contains("Redis", restarted.Services.GetRequiredService<IDistributedCache>().GetType().FullName!, StringComparison.Ordinal);
        using HttpClient selected = Browser(restarted, target.Cookie + "; " + contextCookie);
        selected.DefaultRequestHeaders.Add("X-FoodDiary-Context", contextId);
        await CookieAuthenticationScenario.AssertIdentityAsync(selected, target.Id);
        using HttpResponseMessage restore = await selected.PostAsync("/api/v1/auth/session", content: null);
        Assert.Equal(HttpStatusCode.OK, restore.StatusCode);
        JsonElement session = await restore.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(contextId, session.GetProperty("impersonationContext").GetString());

        using HttpClient exchangeClient = restarted.CreateClient();
        using HttpClient replayClient = concurrent.CreateClient();
        using HttpResponseMessage accepted = await exchangeClient.PostAsJsonAsync("/api/v1/auth/admin-sso/exchange", new { code = ssoCode });
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
        using HttpResponseMessage replay = await replayClient.PostAsJsonAsync("/api/v1/auth/admin-sso/exchange", new { code = ssoCode });
        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);
        using HttpResponseMessage logout = await selected.PostAsJsonAsync("/api/v1/auth/logout", new { });
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        using HttpClient selectedOnReplica = Browser(concurrent, target.Cookie + "; " + contextCookie);
        selectedOnReplica.DefaultRequestHeaders.Add("X-FoodDiary-Context", contextId);
        await CookieAuthenticationScenario.AssertRejectedSessionAsync(selectedOnReplica);
        using HttpClient ordinary = Browser(concurrent, target.Cookie);
        await CookieAuthenticationScenario.AssertIdentityAsync(ordinary, target.Id);
    }

    private WebApplicationFactory<Program> Replica(Dictionary<string, string?> settings) => database.WithWebHostBuilder(builder => {
        foreach ((string key, string? value) in settings) {
            builder.UseSetting(key, value);
        }
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(settings));
    });

    private static HttpClient Browser(WebApplicationFactory<Program> replica, string cookie) {
        HttpClient browser = replica.CreateClient();
        AuthenticationResponseCookies.ApplyBrowserSession(browser, cookie);
        return browser;
    }
}
