using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FoodDiary.Modules.Admin.Presentation.Requests;
using FoodDiary.Modules.Admin.Presentation.Responses;
using FoodDiary.Presentation.Api.Authorization;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

namespace FoodDiary.Web.Api.IntegrationTests;

public sealed partial class PresentationBoundaryIntegrationTests {
    [RequiresDockerFact]
    public async Task AdminDailyAdvices_ImportsPersistsAndSkipsDuplicates() {
        using HttpClient client = CreateAdviceAdminClient();
        string value = $"Advice {Guid.NewGuid():N}";
        var payload = new AdminDailyAdvicesImportHttpRequest(1, [new(value, "en"), new(value, "en-US")]);
        using HttpResponseMessage imported = await PostWithIdempotencyAsync(client, "/api/v1/admin/daily-advices/import", payload);
        Assert.Equal(HttpStatusCode.OK, imported.StatusCode);
        AdminDailyAdvicesImportHttpResponse? result = await imported.Content.ReadFromJsonAsync<AdminDailyAdvicesImportHttpResponse>();
        Assert.NotNull(result);
        Assert.Multiple(() => Assert.Equal(1, result.ImportedCount), () => Assert.Equal(1, result.SkippedCount));
        using HttpResponseMessage repeated = await PostWithIdempotencyAsync(client, "/api/v1/admin/daily-advices/import", payload);
        Assert.Equal(HttpStatusCode.OK, repeated.StatusCode);
        AdminDailyAdvicesImportHttpResponse? replay = await repeated.Content.ReadFromJsonAsync<AdminDailyAdvicesImportHttpResponse>();
        Assert.NotNull(replay);
        Assert.Multiple(() => Assert.Equal(0, replay.ImportedCount), () => Assert.Equal(2, replay.SkippedCount));
        List<AdminDailyAdviceHttpResponse>? items = await client.GetFromJsonAsync<List<AdminDailyAdviceHttpResponse>>("/api/v1/admin/daily-advices");
        Assert.NotNull(items);
        AdminDailyAdviceHttpResponse advice = Assert.Single(items, item => string.Equals(item.Value, value, StringComparison.Ordinal));
        Assert.Multiple(() => Assert.Equal("en", advice.Locale), () => Assert.Equal(1, advice.Weight), () => Assert.Null(advice.Tag));
    }

    [RequiresDockerTheory]
    [InlineData("""{"version":2,"advices":[{"value":"Advice","locale":"en"}]}""")]
    [InlineData("""{"version":1,"advices":null}""")]
    [InlineData("""{"version":1,"advices":[null]}""")]
    [InlineData("""{"version":1,"advices":[]}""")]
    public async Task AdminDailyAdvices_InvalidEnvelopeReturnsBadRequest(string json) {
        using HttpClient client = CreateAdviceAdminClient();
        using var payload = JsonDocument.Parse(json);
        using HttpResponseMessage response = await PostWithIdempotencyAsync(client, "/api/v1/admin/daily-advices/import", payload.RootElement);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [RequiresDockerFact]
    public async Task AdminDailyAdvices_InvalidItemDoesNotPersistValidPrecedingItem() {
        using HttpClient client = CreateAdviceAdminClient();
        string value = $"Rejected advice {Guid.NewGuid():N}";
        using HttpResponseMessage response = await PostWithIdempotencyAsync(client, "/api/v1/admin/daily-advices/import",
            new AdminDailyAdvicesImportHttpRequest(1, [new(value, "en"), new("Invalid", "en", 0)]));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        List<AdminDailyAdviceHttpResponse>? items = await client.GetFromJsonAsync<List<AdminDailyAdviceHttpResponse>>("/api/v1/admin/daily-advices");
        Assert.NotNull(items);
        Assert.DoesNotContain(items, item => string.Equals(item.Value, value, StringComparison.Ordinal));
    }

    [RequiresDockerTheory]
    [InlineData(false, HttpStatusCode.Unauthorized)]
    [InlineData(true, HttpStatusCode.Forbidden)]
    public async Task AdminDailyAdvices_RequiresAdmin(bool authenticated, HttpStatusCode expected) {
        using HttpClient client = testAuthFactory.CreateClient();
        if (authenticated) {
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.AuthenticateHeader, "true");
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, Guid.NewGuid().ToString());
        }
        using HttpResponseMessage list = await client.GetAsync("/api/v1/admin/daily-advices");
        using HttpResponseMessage import = await PostWithIdempotencyAsync(client, "/api/v1/admin/daily-advices/import",
            new AdminDailyAdvicesImportHttpRequest(1, [new("Advice", "en")]));
        Assert.Multiple(() => Assert.Equal(expected, list.StatusCode), () => Assert.Equal(expected, import.StatusCode));
    }

    private HttpClient CreateAdviceAdminClient() {
        HttpClient client = testAuthFactory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.AuthenticateHeader, "true");
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, PresentationRoleNames.Admin);
        return client;
    }
}
