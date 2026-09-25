using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FoodDiary.Presentation.Api.Authorization;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class AdminAiPromptWorkbenchIntegrationTests(TestAuthApiWebApplicationFactory factory)
    : IClassFixture<TestAuthApiWebApplicationFactory> {
    [RequiresDockerFact]
    public async Task CatalogAndPreview_AreUsableWithoutPersistingTemplatesOrCallingAi() {
        using HttpClient client = CreateClient(PresentationRoleNames.Admin);
        using var catalog = JsonDocument.Parse(await client.GetStringAsync("/api/v1/admin/ai-prompts/scenarios"));
        Assert.Equal(8, catalog.RootElement.GetArrayLength());
        foreach (JsonElement scenario in catalog.RootElement.EnumerateArray()) {
            using var format = JsonDocument.Parse(scenario.GetProperty("responseFormatJson").GetString()!);
            Assert.Equal("json_schema", format.RootElement.GetProperty("type").GetString());
            Assert.True(format.RootElement.GetProperty("strict").GetBoolean());
        }
        var draft = new { key = "text-parse", locale = "ru", promptText = "Parse {{userText}}. {{languageHint}}", text = "apple" };
        HttpResponseMessage preview = await client.PostAsJsonAsync("/api/v1/admin/ai-prompts/preview", draft);
        preview.EnsureSuccessStatusCode();
        using var body = JsonDocument.Parse(await preview.Content.ReadAsStringAsync());
        string text = body.RootElement.GetProperty("text").GetString()!;
        Assert.Multiple(() => Assert.Contains("Parse apple", text, StringComparison.Ordinal),
            () => Assert.Contains("language 'ru'", text, StringComparison.Ordinal));
        using var templates = JsonDocument.Parse(await client.GetStringAsync("/api/v1/admin/ai-prompts"));
        Assert.Equal(0, templates.RootElement.GetArrayLength());
        HttpResponseMessage labelPreview = await client.PostAsJsonAsync("/api/v1/admin/ai-prompts/preview",
            new { key = "product-label", locale = "ru", promptText = "Read {{descriptionHint}}. {{languageHint}}", text = "nutrition label" });
        labelPreview.EnsureSuccessStatusCode();
        Assert.Contains("nutrition label", await labelPreview.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [RequiresDockerFact]
    public async Task Preview_RejectsUnsupportedVariables_AndTestRequiresIdempotencyKey() {
        using HttpClient client = CreateClient(PresentationRoleNames.Admin);
        HttpResponseMessage invalid = await client.PostAsJsonAsync("/api/v1/admin/ai-prompts/preview",
            new { key = "vision", locale = "en", promptText = "{{unknown}}" });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        HttpResponseMessage test = await client.PostAsJsonAsync("/api/v1/admin/ai-prompts/test",
            new { key = "text-parse", locale = "en", promptText = "{{userText}}", text = "apple" });
        Assert.Equal(HttpStatusCode.BadRequest, test.StatusCode);
    }

    [RequiresDockerTheory]
    [InlineData("scenarios", false)]
    [InlineData("preview", true)]
    [InlineData("test", true)]
    public async Task Workbench_RequiresAdmin(string action, bool post) {
        using HttpClient client = CreateClient("Premium");
        HttpResponseMessage response = post
            ? await client.PostAsJsonAsync($"/api/v1/admin/ai-prompts/{action}", new { key = "vision", locale = "en", promptText = "Find food" })
            : await client.GetAsync($"/api/v1/admin/ai-prompts/{action}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private HttpClient CreateClient(string role) {
        HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.AuthenticateHeader, "true");
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, role);
        return client;
    }
}
