using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FoodDiary.Modules.Identity.Presentation.Features.Auth.Requests;
using FoodDiary.Modules.Recipes.Presentation.Requests;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class RecipeTimingClearContractTests(TestAuthApiWebApplicationFactory factory)
    : IClassFixture<TestAuthApiWebApplicationFactory> {
    [RequiresDockerFact]
    public async Task PatchTiming_PreservesOmissionClearsExplicitlyAndRetainsZero() {
        using HttpClient client = factory.CreateClient();
        Guid id = await CreateOwnedRecipeAsync(client);
        using var contract = JsonDocument.Parse(await File.ReadAllTextAsync(
            SnapshotPathResolver.GetPath("recipe-timing-clear-contract.json")));

        foreach (JsonElement expected in contract.RootElement.EnumerateArray()) {
            Dictionary<string, object?> payload = CreateUpdatePayload();
            foreach (JsonProperty field in expected.GetProperty("request").EnumerateObject()) {
                payload[field.Name] = field.Value.Clone();
            }

            HttpResponseMessage response = await client.PatchAsJsonAsync($"/api/v1/recipes/{id}", payload);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var read = JsonDocument.Parse(await client.GetStringAsync($"/api/v1/recipes/{id}"));
            Assert.Equal(expected.GetProperty("prepTime").ToString(), read.RootElement.GetProperty("prepTime").ToString());
            Assert.Equal(expected.GetProperty("cookTime").ToString(), read.RootElement.GetProperty("cookTime").ToString());
            Assert.Equal(2, read.RootElement.GetProperty("servings").GetInt32());
            Assert.Single(read.RootElement.GetProperty("steps").EnumerateArray());
        }
    }

    [RequiresDockerTheory]
    [InlineData("prepTime", "clearPrepTime")]
    [InlineData("cookTime", "clearCookTime")]
    public async Task PatchTiming_RejectsClearAndValueWithoutChangingRecipe(string valueField, string clearField) {
        using HttpClient client = factory.CreateClient();
        Guid id = await CreateOwnedRecipeAsync(client);
        Dictionary<string, object?> payload = CreateUpdatePayload();
        payload["name"] = "Must not partially update";
        payload[valueField] = 7;
        payload[clearField] = true;

        HttpResponseMessage response = await client.PatchAsJsonAsync($"/api/v1/recipes/{id}", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var read = JsonDocument.Parse(await client.GetStringAsync($"/api/v1/recipes/{id}"));
        Assert.Equal(12, read.RootElement.GetProperty("prepTime").GetInt32());
        Assert.Equal(15, read.RootElement.GetProperty("cookTime").GetInt32());
        Assert.Equal("Timing clear contract", read.RootElement.GetProperty("name").GetString());
    }

    private static Dictionary<string, object?> CreateUpdatePayload() => new(StringComparer.Ordinal) {
        ["name"] = "Timing clear contract",
        ["servings"] = 2,
        ["calculateNutritionAutomatically"] = true,
        ["steps"] = new[] {
            new RecipeStepHttpRequest(Title: null, "Synthetic timing step", [], ImageUrl: null, ImageAssetId: null),
        },
    };

    private static async Task<Guid> CreateOwnedRecipeAsync(HttpClient client) {
        HttpResponseMessage registration = await client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterHttpRequest($"recipe-clear-{Guid.NewGuid():N}@example.test", "Password123!", "en"));
        registration.EnsureSuccessStatusCode();
        using var registered = JsonDocument.Parse(await registration.Content.ReadAsStringAsync());
        Guid userId = registered.RootElement.GetProperty("user").GetProperty("id").GetGuid();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.AuthenticateHeader, "true");
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, userId.ToString());
        var create = new CreateRecipeHttpRequest("Timing clear contract", Description: null, Comment: null,
            Category: "breakfast", ImageUrl: null, ImageAssetId: null, PrepTime: 12, CookTime: 15, Servings: 2,
            Visibility: "Private", CalculateNutritionAutomatically: true, ManualCalories: null,
            ManualProteins: null, ManualFats: null, ManualCarbs: null, ManualFiber: null, ManualAlcohol: null,
            [new RecipeStepHttpRequest(Title: null, "Synthetic timing step", [], ImageUrl: null, ImageAssetId: null)]) {
            Language = "en", LanguageConfirmed = true,
        };
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/recipes", create);
        response.EnsureSuccessStatusCode();
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("id").GetGuid();
    }
}
