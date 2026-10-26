using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Modules.Identity.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Identity.Presentation.Features.Auth.Requests;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class BrowserCredentialIssuanceApiTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory> {
    [RequiresDockerFact]
    public async Task BrowserRegistrationAndPasswordLogin_DoNotConstructAnUnusedJwtPair() {
        IJwtTokenGenerator generator = Substitute.For<IJwtTokenGenerator>();
        generator.GenerateAccessToken(Arg.Any<UserId>(), Arg.Any<string?>(), Arg.Any<IReadOnlyCollection<string>>(),
                Arg.Any<DateTime?>(), Arg.Any<long>(), Arg.Any<RefreshTokenSessionId>())
            .Returns(_ => throw new InvalidOperationException("Browser credentials must not require a JWT pair."));
        await using WebApplicationFactory<Program> isolated = factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services => {
                services.RemoveAll<IJwtTokenGenerator>();
                services.AddSingleton(generator);
            }));
        using HttpClient browser = isolated.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        string email = $"direct-browser-{Guid.NewGuid():N}@example.com";
        using HttpResponseMessage registered = await browser.PostAsJsonAsync("/api/v1/auth/register", new RegisterHttpRequest(email, "Password123!", "en"));
        Assert.True(registered.StatusCode == HttpStatusCode.OK,
            "Browser sign-in must issue an opaque credential without constructing JWTs.");
        using HttpResponseMessage login = await browser.PostAsJsonAsync("/api/v1/auth/login", new LoginHttpRequest(email, "Password123!"));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Contains("fooddiary.browser=", AuthenticationResponseCookies.ReadBrowserSession(login), StringComparison.Ordinal);
        JsonElement payload = await login.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(payload.TryGetProperty("accessToken", out _));
        Assert.False(payload.TryGetProperty("refreshToken", out _));
        Assert.False(string.IsNullOrWhiteSpace(payload.GetProperty("sessionId").GetString()));
        using HttpResponseMessage restore = await browser.PostAsync("/api/v1/auth/session", content: null);
        Assert.Equal(HttpStatusCode.OK, restore.StatusCode);
        Assert.DoesNotContain(generator.ReceivedCalls(), call => call.GetMethodInfo().Name.StartsWith("Generate", StringComparison.Ordinal));
    }
}
