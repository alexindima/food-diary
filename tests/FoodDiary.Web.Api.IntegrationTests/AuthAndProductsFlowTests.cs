using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FoodDiary.Contracts.Authentication;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;
using Xunit.Abstractions;

namespace FoodDiary.Web.Api.IntegrationTests;

public sealed class AuthAndProductsFlowTests(ApiWebApplicationFactory factory, ITestOutputHelper output)
    : IClassFixture<ApiWebApplicationFactory> {
    private static readonly JsonSerializerOptions JsonOptions = new() {
        PropertyNameCaseInsensitive = true,
    };

    [Fact]
    public async Task Register_ReturnsAuthenticationTokens() {
        var client = factory.CreateClient();
        var email = $"api-tests-{Guid.NewGuid():N}@example.com";
        var request = new RegisterRequest(email, "Password123!", "en");

        var response = await client.PostAsJsonAsync("/api/auth/register", request);
        var body = await response.Content.ReadAsStringAsync();
        output.WriteLine(body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = JsonSerializer.Deserialize<AuthenticationResponse>(body, JsonOptions);
        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(payload.RefreshToken));
        Assert.Equal(email, payload.User.Email);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized() {
        var client = factory.CreateClient();
        var email = $"api-tests-{Guid.NewGuid():N}@example.com";

        var registerResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(email, "Password123!", "en"));
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(email, "WrongPassword123!"));

        Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode);
    }

    [Fact]
    public async Task Products_RequiresAuth_AndReturnsOkWithBearerToken() {
        var client = factory.CreateClient();
        var anonymousResponse = await client.GetAsync("/api/products");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);

        var email = $"api-tests-{Guid.NewGuid():N}@example.com";
        var registerResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(email, "Password123!", "en"));
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        var authPayload = await registerResponse.Content.ReadFromJsonAsync<AuthenticationResponse>(JsonOptions);
        Assert.NotNull(authPayload);
        Assert.False(string.IsNullOrWhiteSpace(authPayload.AccessToken));

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authPayload.AccessToken);
        var authorizedResponse = await client.GetAsync("/api/products");

        Assert.Equal(HttpStatusCode.OK, authorizedResponse.StatusCode);
    }

    [Fact]
    public async Task UsersInfo_RequiresAuth_AndReturnsOkWithBearerToken() {
        var client = factory.CreateClient();
        var anonymousResponse = await client.GetAsync("/api/users/info");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);

        var email = $"api-tests-{Guid.NewGuid():N}@example.com";
        var registerResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(email, "Password123!", "en"));
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        var authPayload = await registerResponse.Content.ReadFromJsonAsync<AuthenticationResponse>(JsonOptions);
        Assert.NotNull(authPayload);
        Assert.False(string.IsNullOrWhiteSpace(authPayload.AccessToken));

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authPayload.AccessToken);
        var authorizedResponse = await client.GetAsync("/api/users/info");

        Assert.NotEqual(HttpStatusCode.Unauthorized, authorizedResponse.StatusCode);
    }
}
