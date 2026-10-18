using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FoodDiary.Modules.Identity.Presentation.Features.Auth.Requests;
using FoodDiary.Modules.Recipes.Presentation.Requests;
using FoodDiary.Presentation.Api.Authorization;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class RecipeTimeLimitContractTests(TestAuthApiWebApplicationFactory factory)
    : IClassFixture<TestAuthApiWebApplicationFactory> {
    [RequiresDockerFact]
    public async Task FiniteTimeLimits_ExcludeUnknownDurationsAndPreserveExplicitZeroAcrossVisibilityScopes() {
        using HttpClient owner = await CreateActorAsync(isAdmin: false);
        using HttpClient publisher = await CreateActorAsync(isAdmin: true);
        using HttpClient guest = factory.CreateClient();
        using var snapshot = JsonDocument.Parse(await File.ReadAllTextAsync(
            SnapshotPathResolver.GetPath("recipe-time-limit-contract.json")));
        string prefix = $"Time limit {Guid.NewGuid():N}";
        (string Name, int? PrepTime, int? CookTime)[] cases = [
            ("unknown", null, null), ("zero", 0, null), ("prep", 12, null),
            ("cook", null, 15), ("combined", 12, 15), ("long", 0, 120),
        ];
        var privateIds = new Dictionary<string, Guid>(StringComparer.Ordinal);
        var publicIds = new Dictionary<string, Guid>(StringComparer.Ordinal);
        var cleanup = new List<(HttpClient Client, Guid Id)>();

        try {
            foreach ((string name, int? prepTime, int? cookTime) in cases) {
                var create = new CreateRecipeHttpRequest($"{prefix} private {name}", Description: null, Comment: null,
                    Category: "breakfast", ImageUrl: null, ImageAssetId: null, prepTime, cookTime, Servings: 1,
                    Visibility: "Private", CalculateNutritionAutomatically: true, ManualCalories: null,
                    ManualProteins: null, ManualFats: null, ManualCarbs: null, ManualFiber: null, ManualAlcohol: null,
                    [new RecipeStepHttpRequest(Title: null, "Synthetic time limit step", [], ImageUrl: null, ImageAssetId: null)]) {
                    Language = "en",
                    LanguageConfirmed = true,
                };
                using HttpResponseMessage created = await owner.PostAsJsonAsync("/api/v1/recipes", create);
                Assert.Equal(HttpStatusCode.Created, created.StatusCode);
                using var createdBody = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
                Guid privateId = createdBody.RootElement.GetProperty("id").GetGuid();
                cleanup.Add((owner, privateId));
                privateIds.Add(name, privateId);
                await AssertPersistedTimesAsync(owner, privateId, prepTime, cookTime);

                var publicId = Guid.NewGuid();
                var catalog = new CatalogRecipeHttpRequest(publicId, $"{prefix} public {name}", Description: null,
                    Category: "breakfast", ImageUrl: null, prepTime, cookTime, Servings: 1, Language: "en",
                    LanguageConfirmed: true, CalculateNutritionAutomatically: true, ManualCalories: null,
                    ManualProteins: null, ManualFats: null, ManualCarbs: null, ManualFiber: null, ManualAlcohol: null,
                    [new CatalogRecipeStepHttpRequest(1, "Synthetic time limit step", Title: null, ImageUrl: null, ImageAssetId: null, [])]);
                using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/admin/catalog/recipes/import") {
                    Content = JsonContent.Create(catalog),
                };
                request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
                using HttpResponseMessage imported = await publisher.SendAsync(request);
                Assert.Equal(HttpStatusCode.OK, imported.StatusCode);
                cleanup.Add((publisher, publicId));
                publicIds.Add(name, publicId);
                using var importBody = JsonDocument.Parse(await imported.Content.ReadAsStringAsync());
                Assert.Equal("imported", importBody.RootElement.GetProperty("status").GetString());
                await AssertPersistedTimesAsync(publisher, publicId, prepTime, cookTime);
            }

            string search = Uri.EscapeDataString(prefix);
            foreach (JsonElement expected in snapshot.RootElement.EnumerateArray()) {
                JsonElement maxTime = expected.GetProperty("MaxTotalTime");
                string filter = maxTime.ValueKind == JsonValueKind.Null
                    ? string.Empty : $"&maxTotalTime={maxTime.GetRawText()}";
                string[] names = [.. expected.GetProperty("Cases").EnumerateArray()
                    .Select(item => item.GetString()!)];
                Guid[] own = [.. names.Select(name => privateIds[name])];
                Guid[] published = [.. names.Select(name => publicIds[name])];

                await AssertPageAsync(owner, $"/api/v1/recipes?includePublic=false&limit=50&search={search}{filter}", own);
                await AssertPageAsync(owner, $"/api/v1/recipes?includePublic=true&limit=50&search={search}{filter}",
                    own.Concat(published));
                await AssertPageAsync(guest, $"/api/v1/recipes/public?limit=50&search={search}{filter}", published);
            }

            await AssertPageAsync(owner, $"/api/v1/recipes?limit=50&search={search}&maxTotalTime=0",
                privateIds.Values.Concat(publicIds.Values));
            using HttpResponseMessage invalid = await guest.GetAsync($"/api/v1/recipes/public?search={search}&maxTotalTime=0");
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);

            using HttpResponseMessage denied = await guest.GetAsync("/api/v1/recipes?maxTotalTime=15");
            Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
            using HttpResponseMessage foreign = await publisher.GetAsync($"/api/v1/recipes/{privateIds["prep"]}");
            Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);
            await AssertPageAsync(publisher, $"/api/v1/recipes?includePublic=false&limit=50&search={search}&maxTotalTime=15",
                new[] { publicIds["zero"], publicIds["prep"], publicIds["cook"] });
        } finally {
            foreach ((HttpClient client, Guid id) in cleanup) {
                using HttpResponseMessage deleted = await client.DeleteAsync($"/api/v1/recipes/{id}");
                Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
                using HttpResponseMessage missing = await client.GetAsync($"/api/v1/recipes/{id}");
                Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
            }
        }
    }

    private async Task<HttpClient> CreateActorAsync(bool isAdmin) {
        HttpClient client = factory.CreateClient();
        using HttpResponseMessage registration = await client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterHttpRequest($"recipe-time-limit-{Guid.NewGuid():N}@example.test", "Password123!", "en"));
        registration.EnsureSuccessStatusCode();
        using var body = JsonDocument.Parse(await registration.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.AuthenticateHeader, "true");
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader,
            body.RootElement.GetProperty("user").GetProperty("id").GetGuid().ToString());
        if (isAdmin) { client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, PresentationRoleNames.Admin); }
        return client;
    }

    private static async Task AssertPersistedTimesAsync(HttpClient client, Guid id, int? prepTime, int? cookTime) {
        using var body = JsonDocument.Parse(await client.GetStringAsync($"/api/v1/recipes/{id}"));
        Assert.Multiple(
            () => Assert.Equal(JsonSerializer.Serialize(prepTime), body.RootElement.GetProperty("prepTime").GetRawText()),
            () => Assert.Equal(JsonSerializer.Serialize(cookTime), body.RootElement.GetProperty("cookTime").GetRawText()));
    }

    private static async Task AssertPageAsync(HttpClient client, string url, IEnumerable<Guid> expectedIds) {
        using HttpResponseMessage response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Guid[] expected = [.. expectedIds.Order()];
        Guid[] actual = [.. body.RootElement.GetProperty("data").EnumerateArray()
            .Select(item => item.GetProperty("id").GetGuid()).Order()];
        Assert.Multiple(
            () => Assert.Equal(expected.Length, body.RootElement.GetProperty("totalItems").GetInt32()),
            () => Assert.Equal(expected, actual));
    }
}
