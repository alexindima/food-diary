using System.Net;
using System.Text;
using System.Text.Json;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class BackendDisclosureTransportTests(TransportApiWebApplicationFactory factory)
    : IClassFixture<TransportApiWebApplicationFactory> {
    [Fact]
    public async Task PublicVersionRoutes_ReturnNotFoundAsync() {
        using JsonDocument contract = await ReadContractAsync();
        using HttpClient client = factory.CreateClient();
        foreach (JsonElement path in contract.RootElement.GetProperty("publicVersionRoutes").EnumerateArray()) {
            using HttpResponseMessage response = await client.GetAsync(path.GetString());
            Assert.Equal(contract.RootElement.GetProperty("publicVersionStatus").GetInt32(), (int)response.StatusCode);
        }
    }

    [Fact]
    public async Task InternalVersion_PreservesDeploymentMetadataAsync() {
        await using WebApplicationFactory<Program> versionedFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>(StringComparer.Ordinal) {
                    ["BuildInfo:CommitSha"] = "abc123def456",
                    ["BuildInfo:ImageTag"] = "sha-abc123def456",
                })));
        using JsonDocument contract = await ReadContractAsync();
        using HttpClient client = versionedFactory.CreateClient();
        using HttpResponseMessage response = await client.GetAsync(contract.RootElement.GetProperty("internalVersionRoute").GetString());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        JsonElement root = payload.RootElement;
        string[] keys = [.. root.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal)];
        string[] expectedKeys = [.. contract.RootElement.GetProperty("metadataKeys").EnumerateArray().Select(value => value.GetString()!)];
        Assert.Multiple(
            () => Assert.Equal(expectedKeys, keys),
            () => Assert.Equal("abc123def456", root.GetProperty("commitSha").GetString()),
            () => Assert.Equal("sha-abc123def456", root.GetProperty("imageTag").GetString()),
            () => Assert.Equal("Development", root.GetProperty("environment").GetString()),
            () => Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("applicationVersion").GetString())),
            () => Assert.True(root.GetProperty("startedAtUtc").GetDateTimeOffset() > DateTimeOffset.MinValue),
            () => Assert.True(response.Headers.CacheControl?.NoStore));
    }

    [Theory]
    [InlineData("{\"email\":123,\"password\":\"Password123!\"}")]
    [InlineData("{\"email\":{},\"password\":\"Password123!\"}")]
    [InlineData("[]")]
    [InlineData("{\"email\":")]
    public async Task InvalidJson_OmitsImplementationDiagnosticsAsync(string body) {
        using JsonDocument contract = await ReadContractAsync();
        using HttpClient client = factory.CreateClient();
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        using HttpResponseMessage response = await client.PostAsync("/api/v1/auth/register", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        string responseBody = await response.Content.ReadAsStringAsync();
        using var payload = JsonDocument.Parse(responseBody);
        JsonElement root = payload.RootElement;
        Assert.Multiple(
            () => Assert.Equal(contract.RootElement.GetProperty("jsonError").GetString(), root.GetProperty("error").GetString()),
            () => Assert.Equal(contract.RootElement.GetProperty("jsonMessage").GetString(), root.GetProperty("message").GetString()),
            () => Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("traceId").GetString())),
            () => Assert.NotEmpty(root.GetProperty("errors").EnumerateObject()));
        foreach (JsonElement forbidden in contract.RootElement.GetProperty("forbiddenJsonDiagnostics").EnumerateArray()) {
            Assert.DoesNotContain(forbidden.GetString()!, responseBody, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task RequiredFields_KeepUsefulValidationMessagesAsync() {
        using HttpClient client = factory.CreateClient();
        using var content = new StringContent("{\"email\":\"\",\"password\":\"123\"}", Encoding.UTF8, "application/json");
        using HttpResponseMessage response = await client.PostAsync("/api/v1/auth/register", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        JsonElement errors = payload.RootElement.GetProperty("errors");
        Assert.Multiple(
            () => Assert.Contains(errors.GetProperty("email").EnumerateArray(), value => value.GetString()!.Contains("required", StringComparison.OrdinalIgnoreCase)),
            () => Assert.NotEmpty(errors.GetProperty("password").EnumerateArray()));
    }

    [Fact]
    public async Task KestrelResponse_OmitsServerHeaderAsync() {
        await using var socketFactory = new TransportApiWebApplicationFactory();
        socketFactory.UseKestrel(0);
        using HttpClient client = socketFactory.CreateClient();
        using HttpResponseMessage response = await client.GetAsync("/health/live");
        Assert.Multiple(
            () => Assert.Equal(HttpStatusCode.OK, response.StatusCode),
            () => Assert.Empty(response.Headers.Server));
    }

    private static async Task<JsonDocument> ReadContractAsync() =>
        JsonDocument.Parse(await File.ReadAllTextAsync(SnapshotPathResolver.GetPath("backend-disclosure-contract.json")));
}
