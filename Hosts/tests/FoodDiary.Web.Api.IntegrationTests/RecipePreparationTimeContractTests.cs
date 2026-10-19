using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FoodDiary.Modules.Identity.Presentation.Features.Auth.Requests;
using FoodDiary.Modules.Recipes.Presentation.Requests;
using FoodDiary.Presentation.Api.Authorization;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class RecipePreparationTimeContractTests(TestAuthApiWebApplicationFactory factory)
    : IClassFixture<TestAuthApiWebApplicationFactory> {
    [RequiresDockerFact]
    public async Task CreateAndCatalogImport_PreserveMissingZeroAndPositivePreparationTimes() {
        using HttpClient client = factory.CreateClient();
        HttpResponseMessage registration = await client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterHttpRequest($"recipe-time-{Guid.NewGuid():N}@example.test", "Password123!", "en"));
        registration.EnsureSuccessStatusCode();
        using var registered = JsonDocument.Parse(await registration.Content.ReadAsStringAsync());
        Guid userId = registered.RootElement.GetProperty("user").GetProperty("id").GetGuid();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.AuthenticateHeader, "true");
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, userId.ToString());
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, PresentationRoleNames.Admin);
        using var snapshot = JsonDocument.Parse(await File.ReadAllTextAsync(
            SnapshotPathResolver.GetPath("recipe-preparation-time-contract.json")));

        foreach (JsonElement expected in snapshot.RootElement.EnumerateArray()) {
            int? prepTime = expected.GetProperty("PrepTime").ValueKind == JsonValueKind.Null
                ? null : expected.GetProperty("PrepTime").GetInt32();
            var create = new CreateRecipeHttpRequest("Preparation time contract", Description: null, Comment: null,
                Category: "breakfast", ImageUrl: null, ImageAssetId: null, prepTime, CookTime: null, Servings: 1,
                Visibility: "Private", CalculateNutritionAutomatically: true, ManualCalories: null,
                ManualProteins: null, ManualFats: null, ManualCarbs: null, ManualFiber: null, ManualAlcohol: null,
                [new RecipeStepHttpRequest(Title: null, "Synthetic contract step", [], ImageUrl: null, ImageAssetId: null)]) {
                Language = "en",
                LanguageConfirmed = true,
            };
            HttpResponseMessage created = await client.PostAsJsonAsync("/api/v1/recipes", create);
            Assert.Equal(expected.GetProperty("CreateStatus").GetInt32(), (int)created.StatusCode);
            using var createdBody = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
            Guid createdId = createdBody.RootElement.GetProperty("id").GetGuid();
            AssertTime(expected, createdBody.RootElement);
            using var createdRead = JsonDocument.Parse(await client.GetStringAsync($"/api/v1/recipes/{createdId}"));
            AssertTime(expected, createdRead.RootElement);

            var importedId = Guid.NewGuid();
            var catalog = new CatalogRecipeHttpRequest(importedId, "Catalog preparation time contract", Description: null,
                Category: "breakfast", ImageUrl: null, prepTime, CookTime: null, Servings: 1, Language: "en",
                LanguageConfirmed: true, CalculateNutritionAutomatically: true, ManualCalories: null,
                ManualProteins: null, ManualFats: null, ManualCarbs: null, ManualFiber: null, ManualAlcohol: null,
                [new CatalogRecipeStepHttpRequest(1, "Synthetic contract step", Title: null, ImageUrl: null, ImageAssetId: null, [])]);
            HttpResponseMessage preview = await client.PostAsJsonAsync("/api/v1/admin/catalog/recipes/preview", catalog);
            preview.EnsureSuccessStatusCode();
            using var previewBody = JsonDocument.Parse(await preview.Content.ReadAsStringAsync());
            Assert.Equal("ready", previewBody.RootElement.GetProperty("status").GetString());
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/recipes/{importedId}")).StatusCode);
            using var importRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/admin/catalog/recipes/import") {
                Content = JsonContent.Create(catalog),
            };
            importRequest.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
            HttpResponseMessage imported = await client.SendAsync(importRequest);
            Assert.Equal(expected.GetProperty("ImportStatus").GetInt32(), (int)imported.StatusCode);
            using var importedBody = JsonDocument.Parse(await imported.Content.ReadAsStringAsync());
            Assert.Equal("imported", importedBody.RootElement.GetProperty("status").GetString());
            using var importedRead = JsonDocument.Parse(await client.GetStringAsync($"/api/v1/recipes/{importedId}"));
            AssertTime(expected, importedRead.RootElement);
            using var exported = JsonDocument.Parse(await client.GetStringAsync("/api/v1/admin/catalog/recipes/export"));
            JsonElement exportedRecipe = Assert.Single(exported.RootElement.EnumerateArray(), item => item.GetProperty("id").GetGuid() == importedId);
            AssertTime(expected, exportedRecipe);

            foreach (Guid id in new[] { createdId, importedId }) {
                Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/v1/recipes/{id}")).StatusCode);
                Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/recipes/{id}")).StatusCode);
            }
        }
    }

    private static void AssertTime(JsonElement expected, JsonElement actual) => Assert.Multiple(
        () => Assert.Equal(expected.GetProperty("PrepTime").GetRawText(), actual.GetProperty("prepTime").GetRawText()),
        () => Assert.Equal(JsonValueKind.Null, actual.GetProperty("cookTime").ValueKind));
}
