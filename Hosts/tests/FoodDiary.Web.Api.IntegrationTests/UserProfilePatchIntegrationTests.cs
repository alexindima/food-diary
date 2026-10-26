using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FoodDiary.Modules.Identity.Presentation.Features.Auth.Requests;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class UserProfilePatchIntegrationTests(PostgresApiWebApplicationFactory factory)
    : IClassFixture<PostgresApiWebApplicationFactory> {
    [RequiresDockerTheory]
    [InlineData("omitted")]
    [InlineData("cleared")]
    [InlineData("replaced")]
    [InlineData("ignoredMetadata")]
    public async Task UpdateHeight_PersistsPatchContractAgainstPostgres(string scenario) {
        using var snapshot = JsonDocument.Parse(await File.ReadAllTextAsync(
            SnapshotPathResolver.GetPath("user-height-patch-contract.json")));
        JsonElement contract = snapshot.RootElement.GetProperty(scenario);
        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage registered = await client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterHttpRequest($"height-{Guid.NewGuid():N}@example.com", "Password123!", "en"));
        Assert.Equal(HttpStatusCode.OK, registered.StatusCode);
        using var auth = JsonDocument.Parse(await registered.Content.ReadAsStringAsync());
        AuthenticationResponseCookies.ApplyBrowserSession(client, AuthenticationResponseCookies.ReadBrowserSession(registered));

        using HttpResponseMessage initialized = await client.PatchAsJsonAsync("/api/v1/users/info",
            new { heightCm = 170, firstName = "QA" });
        Assert.Equal(HttpStatusCode.OK, initialized.StatusCode);
        for (int attempt = 0; attempt < 2; attempt++) {
            using HttpResponseMessage updated = await client.PatchAsJsonAsync("/api/v1/users/info", contract.GetProperty("request"));
            Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
            using var saved = JsonDocument.Parse(await updated.Content.ReadAsStringAsync());
            AssertHeightContract(saved.RootElement, contract.GetProperty("response"));
        }
        using HttpResponseMessage reloaded = await client.GetAsync("/api/v1/users/info");
        Assert.Equal(HttpStatusCode.OK, reloaded.StatusCode);
        using var profile = JsonDocument.Parse(await reloaded.Content.ReadAsStringAsync());
        AssertHeightContract(profile.RootElement, contract.GetProperty("response"));
    }

    private static void AssertHeightContract(JsonElement actual, JsonElement expected) {
        double? expectedHeight = expected.GetProperty("heightCm").ValueKind == JsonValueKind.Null
            ? null : expected.GetProperty("heightCm").GetDouble();
        double? actualHeight = actual.GetProperty("heightCm").ValueKind == JsonValueKind.Null
            ? null : actual.GetProperty("heightCm").GetDouble();
        Assert.Multiple(
            () => Assert.Equal(expectedHeight, actualHeight),
            () => Assert.Equal(expected.GetProperty("firstName").GetString(), actual.GetProperty("firstName").GetString()));
    }

    [RequiresDockerTheory]
    [InlineData("omitted")]
    [InlineData("clearedUrl")]
    [InlineData("clearedAsset")]
    public async Task UpdateAvatar_PersistsPatchContractAgainstPostgres(string scenario) {
        using var snapshot = JsonDocument.Parse(await File.ReadAllTextAsync(
            SnapshotPathResolver.GetPath("user-avatar-patch-contract.json")));
        JsonElement contract = snapshot.RootElement.GetProperty(scenario);
        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage registered = await client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterHttpRequest($"avatar-{Guid.NewGuid():N}@example.com", "Password123!", "en"));
        Assert.Equal(HttpStatusCode.OK, registered.StatusCode);
        using var auth = JsonDocument.Parse(await registered.Content.ReadAsStringAsync());
        AuthenticationResponseCookies.ApplyBrowserSession(client, AuthenticationResponseCookies.ReadBrowserSession(registered));

        using HttpResponseMessage initialized = await client.PatchAsJsonAsync("/api/v1/users/info",
            new { profileImage = "https://example.test/avatar.png", firstName = "QA" });
        Assert.Equal(HttpStatusCode.OK, initialized.StatusCode);
        using HttpResponseMessage updated = await client.PatchAsJsonAsync("/api/v1/users/info", contract.GetProperty("request"));
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        using HttpResponseMessage reloaded = await client.GetAsync("/api/v1/users/info");
        Assert.Equal(HttpStatusCode.OK, reloaded.StatusCode);
        using var profile = JsonDocument.Parse(await reloaded.Content.ReadAsStringAsync());
        JsonElement expected = contract.GetProperty("response");

        Assert.Multiple(
            () => Assert.Equal(expected.GetProperty("profileImage").GetString(), profile.RootElement.GetProperty("profileImage").GetString()),
            () => Assert.Equal(JsonValueKind.Null, profile.RootElement.GetProperty("profileImageAssetId").ValueKind));
    }

    [RequiresDockerTheory]
    [InlineData("omitted")]
    [InlineData("cleared")]
    [InlineData("replaced")]
    public async Task UpdateBirthDate_PersistsPatchContractAgainstPostgres(string scenario) {
        using var snapshot = JsonDocument.Parse(await File.ReadAllTextAsync(
            SnapshotPathResolver.GetPath("user-birthdate-patch-contract.json")));
        JsonElement contract = snapshot.RootElement.GetProperty(scenario);
        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage registered = await client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterHttpRequest($"birthdate-{Guid.NewGuid():N}@example.com", "Password123!", "en"));
        Assert.Equal(HttpStatusCode.OK, registered.StatusCode);
        using var auth = JsonDocument.Parse(await registered.Content.ReadAsStringAsync());
        AuthenticationResponseCookies.ApplyBrowserSession(client, AuthenticationResponseCookies.ReadBrowserSession(registered));

        using HttpResponseMessage initialized = await client.PatchAsJsonAsync("/api/v1/users/info",
            new { birthDate = new DateTime(2000, 10, 2, 0, 0, 0, DateTimeKind.Utc), firstName = "QA" });
        Assert.Equal(HttpStatusCode.OK, initialized.StatusCode);
        using HttpResponseMessage updated = await client.PatchAsJsonAsync("/api/v1/users/info", contract.GetProperty("request"));
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        using HttpResponseMessage reloaded = await client.GetAsync("/api/v1/users/info");
        Assert.Equal(HttpStatusCode.OK, reloaded.StatusCode);
        using var profile = JsonDocument.Parse(await reloaded.Content.ReadAsStringAsync());
        JsonElement expected = contract.GetProperty("response");
        DateTime? expectedDate = expected.GetProperty("birthDate").ValueKind == JsonValueKind.Null
            ? null : expected.GetProperty("birthDate").GetDateTime();
        DateTime? actualDate = profile.RootElement.GetProperty("birthDate").ValueKind == JsonValueKind.Null
            ? null : profile.RootElement.GetProperty("birthDate").GetDateTime();

        Assert.Multiple(
            () => Assert.Equal(expectedDate, actualDate),
            () => Assert.Equal(expected.GetProperty("firstName").GetString(), profile.RootElement.GetProperty("firstName").GetString()));
    }
}
