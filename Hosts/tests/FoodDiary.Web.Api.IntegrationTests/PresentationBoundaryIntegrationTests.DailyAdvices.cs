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
        using HttpResponseMessage groups = await client.GetAsync("/api/v1/admin/daily-advices/groups");
        var groupId = Guid.NewGuid();
        using HttpResponseMessage pairs = await PostWithIdempotencyAsync(client, "/api/v1/admin/daily-advices/groups/import",
            new AdminDailyAdvicePairsImportHttpRequest(2, [new(groupId, "Russian", "English")]));
        using HttpResponseMessage edit = await client.PutAsJsonAsync($"/api/v1/admin/daily-advices/groups/{groupId}",
            new AdminDailyAdviceGroupUpdateHttpRequest("Russian", "English"));
        using HttpResponseMessage delete = await client.DeleteAsync($"/api/v1/admin/daily-advices/groups/{groupId}");
        Assert.Multiple(() => Assert.Equal(expected, list.StatusCode), () => Assert.Equal(expected, import.StatusCode),
            () => Assert.Equal(expected, groups.StatusCode), () => Assert.Equal(expected, pairs.StatusCode),
            () => Assert.Equal(expected, edit.StatusCode), () => Assert.Equal(expected, delete.StatusCode));
    }

    [RequiresDockerFact]
    public async Task AdminDailyAdviceGroups_LinkLegacyEditAndDeleteBothTranslations() {
        using HttpClient client = CreateAdviceAdminClient();
        var groupId = Guid.NewGuid();
        string ru = $"Russian {groupId:N}";
        string en = $"English {groupId:N}";
        using HttpResponseMessage legacy = await PostWithIdempotencyAsync(client, "/api/v1/admin/daily-advices/import",
            new AdminDailyAdvicesImportHttpRequest(1, [new(ru, "ru"), new(en, "en")]));
        Assert.Equal(HttpStatusCode.OK, legacy.StatusCode);
        var payload = new AdminDailyAdvicePairsImportHttpRequest(2, [new(groupId, ru, en)]);
        using HttpResponseMessage imported = await PostWithIdempotencyAsync(client, "/api/v1/admin/daily-advices/groups/import", payload);
        Assert.Equal(HttpStatusCode.OK, imported.StatusCode);
        List<AdminDailyAdviceGroupHttpResponse>? groups = await client.GetFromJsonAsync<List<AdminDailyAdviceGroupHttpResponse>>("/api/v1/admin/daily-advices/groups");
        Assert.NotNull(groups);
        AdminDailyAdviceGroupHttpResponse group = Assert.Single(groups, item => item.Id == groupId);
        Assert.Multiple(() => Assert.Equal(ru, group.Ru), () => Assert.Equal(en, group.En));
        using HttpResponseMessage edited = await client.PutAsJsonAsync($"/api/v1/admin/daily-advices/groups/{groupId}",
            new AdminDailyAdviceGroupUpdateHttpRequest(ru + " edited", en + " edited", 3, "habit"));
        Assert.Equal(HttpStatusCode.OK, edited.StatusCode);
        List<AdminDailyAdviceHttpResponse>? rows = await client.GetFromJsonAsync<List<AdminDailyAdviceHttpResponse>>("/api/v1/admin/daily-advices");
        Assert.NotNull(rows);
        Assert.Contains(rows, item => string.Equals(item.Value, ru + " edited", StringComparison.Ordinal) && item.Weight == 3);
        Assert.Contains(rows, item => string.Equals(item.Value, en + " edited", StringComparison.Ordinal) && item.Weight == 3);
        using HttpResponseMessage deleted = await client.DeleteAsync($"/api/v1/admin/daily-advices/groups/{groupId}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        rows = await client.GetFromJsonAsync<List<AdminDailyAdviceHttpResponse>>("/api/v1/admin/daily-advices");
        Assert.NotNull(rows);
        Assert.DoesNotContain(rows, item => item.Value.Contains(groupId.ToString("N"), StringComparison.Ordinal));
    }

    private HttpClient CreateAdviceAdminClient() {
        HttpClient client = testAuthFactory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.AuthenticateHeader, "true");
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, PresentationRoleNames.Admin);
        return client;
    }
}
