using System.Net;
using System.Text.Json;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class RecipeVideoTransportIntegrationTests(TransportApiWebApplicationFactory factory) : IClassFixture<TransportApiWebApplicationFactory> {
    private const string Endpoint = "/api/v1/ai/food/recipe-import/video";

    [Fact]
    public async Task SwaggerVideoEndpoint_HasMultipartContract_AndMatchesFullSnapshot() {
        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage response = await client.GetAsync("/swagger/v1/swagger.json");
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        JsonElement operation = document.RootElement.GetProperty("paths").GetProperty("/api/v{version}/ai/food/recipe-import/video").GetProperty("post");
        JsonElement form = operation.GetProperty("requestBody").GetProperty("content").GetProperty("multipart/form-data").GetProperty("schema").GetProperty("properties");
        Assert.Multiple(
            () => Assert.True(operation.GetProperty("requestBody").GetProperty("content").TryGetProperty("multipart/form-data", out _)),
            () => Assert.Equal("binary", form.GetProperty("video").GetProperty("format").GetString()),
            () => Assert.Equal("string", form.GetProperty("sourceUrl").GetProperty("type").GetString()),
            () => Assert.Equal("string", form.GetProperty("text").GetProperty("type").GetString()),
            () => Assert.True(operation.GetProperty("responses").TryGetProperty("200", out _)),
            () => Assert.True(operation.GetProperty("responses").TryGetProperty("413", out _)),
            () => Assert.True(operation.GetProperty("responses").TryGetProperty("429", out _)));
        await PresentationBoundaryIntegrationTests.AssertSnapshotAsync("openapi-full-contract.json", PresentationBoundaryIntegrationTests.BuildFullOpenApiSnapshot(document.RootElement));
        await PresentationBoundaryIntegrationTests.AssertSnapshotAsync("openapi-focused-contract.json", PresentationBoundaryIntegrationTests.BuildFocusedOpenApiSnapshot(document.RootElement));
    }

    [Fact]
    public async Task MultipartVideo_WithoutIdempotencyKey_ReturnsStandardErrorBeforeUseCase() {
        using HttpClient client = factory.CreateClient();
        Authenticate(client, "Premium");
        using var content = new MultipartFormDataContent {
            { new ByteArrayContent([1, 2, 3]), "video", "recipe.mp4" },
            { new StringContent("caption"), "text" },
        };
        using HttpResponseMessage response = await client.PostAsync(Endpoint, content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Idempotency.Required", document.RootElement.GetProperty("error").GetString());
    }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("User", HttpStatusCode.Forbidden)]
    public async Task VideoEndpoint_RequiresPremium(string? role, HttpStatusCode expected) {
        using HttpClient client = factory.CreateClient();
        if (role is not null) {
            Authenticate(client, role);
        }
        using var content = new MultipartFormDataContent { { new StringContent("https://example.org/video"), "sourceUrl" } };
        using HttpResponseMessage response = await client.PostAsync(Endpoint, content);
        Assert.Equal(expected, response.StatusCode);
    }

    private static void Authenticate(HttpClient client, string role) {
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.AuthenticateHeader, "true");
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, Guid.NewGuid().ToString("D"));
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, role);
    }
}
