using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FoodDiary.Presentation.Api.Features.Auth.Requests;
using FoodDiary.Presentation.Api.Features.Products.Requests;
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
        var request = new RegisterHttpRequest(email, "Password123!", "en");

        var response = await client.PostAsJsonAsync("/api/auth/register", request);
        var body = await response.Content.ReadAsStringAsync();
        output.WriteLine(body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = JsonSerializer.Deserialize<AuthPayload>(body, JsonOptions);
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
            new RegisterHttpRequest(email, "Password123!", "en"));
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginHttpRequest(email, "WrongPassword123!"));

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
            new RegisterHttpRequest(email, "Password123!", "en"));
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        var authPayload = await registerResponse.Content.ReadFromJsonAsync<AuthPayload>(JsonOptions);
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
            new RegisterHttpRequest(email, "Password123!", "en"));
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        var authPayload = await registerResponse.Content.ReadFromJsonAsync<AuthPayload>(JsonOptions);
        Assert.NotNull(authPayload);
        Assert.False(string.IsNullOrWhiteSpace(authPayload.AccessToken));

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authPayload.AccessToken);
        var authorizedResponse = await client.GetAsync("/api/users/info");

        Assert.NotEqual(HttpStatusCode.Unauthorized, authorizedResponse.StatusCode);
    }

    [Fact]
    public async Task CreateProduct_ReturnsCreatedAndLocationHeader() {
        var client = factory.CreateClient();
        var email = $"api-tests-{Guid.NewGuid():N}@example.com";
        var registerResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterHttpRequest(email, "Password123!", "en"));
        registerResponse.EnsureSuccessStatusCode();

        var authPayload = await registerResponse.Content.ReadFromJsonAsync<AuthPayload>(JsonOptions);
        Assert.NotNull(authPayload);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authPayload.AccessToken);

        var response = await client.PostAsJsonAsync(
            "/api/products",
            new CreateProductHttpRequest(
                null,
                "Created Product",
                null,
                "Unknown",
                null,
                null,
                null,
                null,
                null,
                "G",
                100,
                100,
                120,
                10,
                5,
                20,
                3,
                0,
                "Private"));

        var payload = await response.Content.ReadFromJsonAsync<ProductPayload>(JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(payload);
        Assert.NotNull(response.Headers.Location);
        Assert.EndsWith($"/api/Products/{payload.Id}", response.Headers.Location.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private sealed record AuthPayload(string AccessToken, string RefreshToken, AuthUserPayload User);

    private sealed record AuthUserPayload(string Email);

    private sealed record ProductPayload(Guid Id);
}
