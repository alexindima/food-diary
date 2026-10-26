using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Modules.Identity.Infrastructure.Authentication;
using FoodDiary.Modules.Identity.Presentation.Features.Auth.Requests;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class PasswordLoginThrottlingApiTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory> {
    [RequiresDockerFact]
    public async Task AccountCooldown_PreservesOtherAccounts_AndResetsAfterCorrectPasswordAsync() {
        var clock = new ThrottleClock();
        await using WebApplicationFactory<Program> app = factory.WithWebHostBuilder(builder => {
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>(StringComparer.Ordinal) { ["PasswordLoginThrottle:FailureThreshold"] = "3" }));
            builder.ConfigureTestServices(services => services.Replace(ServiceDescriptor.Singleton<IPasswordLoginThrottle>(provider =>
                new InMemoryPasswordLoginThrottle(provider.GetRequiredService<IOptions<PasswordLoginThrottleOptions>>(), clock))));
        });
        using HttpClient client = app.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        string email = $"throttle-{Guid.NewGuid():N}@example.com";
        using HttpResponseMessage registration = await client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterHttpRequest(email, "Password123!", "en"));
        Assert.Equal(HttpStatusCode.OK, registration.StatusCode);

        for (int index = 0; index < 3; index++) {
            using HttpResponseMessage failure = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginHttpRequest(email, "WrongPassword123!"));
            Assert.Equal(HttpStatusCode.Unauthorized, failure.StatusCode);
        }
        using HttpResponseMessage blocked = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginHttpRequest(email, "Password123!"));
        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
        Assert.Equal(TimeSpan.FromSeconds(30), blocked.Headers.RetryAfter?.Delta);
        using var error = JsonDocument.Parse(await blocked.Content.ReadAsStringAsync());
        Assert.Multiple(
            () => Assert.Equal("RateLimit.Exceeded", error.RootElement.GetProperty("error").GetString()),
            () => Assert.False(error.RootElement.TryGetProperty("errors", out _)),
            () => Assert.False(string.IsNullOrEmpty(error.RootElement.GetProperty("traceId").GetString())));
        using HttpResponseMessage other = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginHttpRequest($"missing-{Guid.NewGuid():N}@example.com", "WrongPassword123!"));
        Assert.Equal(HttpStatusCode.Unauthorized, other.StatusCode);

        clock.Advance(TimeSpan.FromSeconds(30));
        using HttpResponseMessage success = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginHttpRequest(email, "Password123!"));
        Assert.Equal(HttpStatusCode.OK, success.StatusCode);
        for (int index = 0; index < 3; index++) {
            using HttpResponseMessage failure = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginHttpRequest(email, "WrongPassword123!"));
            Assert.Equal(HttpStatusCode.Unauthorized, failure.StatusCode);
        }
        using HttpResponseMessage resetCooldown = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginHttpRequest(email, "wrong"));
        Assert.Equal(HttpStatusCode.TooManyRequests, resetCooldown.StatusCode);
        Assert.Equal(TimeSpan.FromSeconds(30), resetCooldown.Headers.RetryAfter?.Delta);
    }

    [ExcludeFromCodeCoverage]
    private sealed class ThrottleClock : TimeProvider {
        private DateTimeOffset _now = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan duration) => _now += duration;
    }
}
