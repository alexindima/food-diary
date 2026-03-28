using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FoodDiary.Presentation.Api.Features.Auth.Requests;
using FoodDiary.Presentation.Api.Features.Consumptions.Requests;
using FoodDiary.Presentation.Api.Features.Hydration.Requests;
using FoodDiary.Presentation.Api.Features.Products.Requests;
using FoodDiary.Presentation.Api.Features.Recipes.Requests;
using FoodDiary.Presentation.Api.Features.WaistEntries.Requests;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

namespace FoodDiary.Web.Api.IntegrationTests;

public sealed class PostgresUserFlowTests(PostgresApiWebApplicationFactory factory)
    : IClassFixture<PostgresApiWebApplicationFactory> {
    private static readonly JsonSerializerOptions JsonOptions = new() {
        PropertyNameCaseInsensitive = true,
    };

    [RequiresDockerFact]
    public async Task CreateProduct_ThenCreateConsumption_ThenGetDashboard_ReturnsNutritionData() {
        var client = await CreateAuthenticatedClientAsync();

        var createProductResponse = await client.PostAsJsonAsync("/api/products", new CreateProductHttpRequest(
            null, "Integration Apple", null, "Unknown", null, null, null, null, null,
            "G", 100, 100, 52, 0.3, 0.2, 14, 2.4, 0, "Private"));
        await AssertStatusCodeAsync(HttpStatusCode.Created, createProductResponse);
        var product = await createProductResponse.Content.ReadFromJsonAsync<IdPayload>(JsonOptions);
        Assert.NotNull(product);

        var today = DateTime.UtcNow.Date;
        var createConsumptionResponse = await client.PostAsJsonAsync("/api/consumptions", new CreateConsumptionHttpRequest(
            today, "Lunch", null, null, null,
            [new ConsumptionItemHttpRequest(product.Id, null, 200)],
            IsNutritionAutoCalculated: true));
        await AssertStatusCodeAsync(HttpStatusCode.Created, createConsumptionResponse);

        var dashboardResponse = await client.GetAsync($"/api/dashboard?date={today:yyyy-MM-dd}");
        await AssertStatusCodeAsync(HttpStatusCode.OK, dashboardResponse);
        var dashboard = await dashboardResponse.Content.ReadFromJsonAsync<DashboardPayload>(JsonOptions);

        Assert.NotNull(dashboard);
        Assert.True(dashboard.Statistics.TotalCalories > 0, "Dashboard should show calories from the consumption");
    }

    [RequiresDockerFact]
    public async Task CreateHydrationEntries_ThenGetDailyTotal_ReturnsAggregatedAmount() {
        var client = await CreateAuthenticatedClientAsync();
        var now = DateTime.UtcNow;

        var response1 = await client.PostAsJsonAsync("/api/hydration", new CreateHydrationEntryHttpRequest(now, 250));
        var response2 = await client.PostAsJsonAsync("/api/hydration", new CreateHydrationEntryHttpRequest(now.AddMinutes(30), 500));
        await AssertStatusCodeAsync(HttpStatusCode.Created, response1);
        await AssertStatusCodeAsync(HttpStatusCode.Created, response2);

        var totalResponse = await client.GetAsync($"/api/hydration/daily-total?date={now:yyyy-MM-dd}");
        await AssertStatusCodeAsync(HttpStatusCode.OK, totalResponse);
        var total = await totalResponse.Content.ReadFromJsonAsync<HydrationDailyTotalPayload>(JsonOptions);

        Assert.NotNull(total);
        Assert.True(total.TotalMl >= 750, $"Expected at least 750ml, got {total.TotalMl}");
    }

    [RequiresDockerFact]
    public async Task CreateWaistEntry_WithDuplicateDate_ReturnsConflict() {
        var client = await CreateAuthenticatedClientAsync();
        var request = new CreateWaistEntryHttpRequest(
            new DateTime(2026, 3, 27, 0, 0, 0, DateTimeKind.Utc), 80.0);

        var first = await client.PostAsJsonAsync("/api/waist-entries", request);
        var duplicate = await client.PostAsJsonAsync("/api/waist-entries", request);
        var payload = await duplicate.Content.ReadFromJsonAsync<ErrorPayload>(JsonOptions);

        await AssertStatusCodeAsync(HttpStatusCode.Created, first);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("WaistEntry.AlreadyExists", payload.Error);
    }

    [RequiresDockerFact]
    public async Task CreateRecipe_ThenDuplicate_CreatesTwoIndependentRecipes() {
        var client = await CreateAuthenticatedClientAsync();

        var createProductResponse = await client.PostAsJsonAsync("/api/products", new CreateProductHttpRequest(
            null, "Recipe Ingredient", null, "Unknown", null, null, null, null, null,
            "G", 100, 100, 100, 5, 3, 15, 1, 0, "Private"));
        await AssertStatusCodeAsync(HttpStatusCode.Created, createProductResponse);
        var product = await createProductResponse.Content.ReadFromJsonAsync<IdPayload>(JsonOptions);
        Assert.NotNull(product);

        var createRecipeResponse = await client.PostAsJsonAsync("/api/recipes", new CreateRecipeHttpRequest(
            "Original Soup", "Test recipe", null, "Dinner", null, null,
            15, 30, 2, "private", true, null, null, null, null, null, null,
            [new RecipeStepHttpRequest("Boil", "Boil water",
                [new RecipeIngredientHttpRequest(product.Id, null, 200)], null, null)]));
        await AssertStatusCodeAsync(HttpStatusCode.Created, createRecipeResponse);
        var recipe = await createRecipeResponse.Content.ReadFromJsonAsync<IdPayload>(JsonOptions);
        Assert.NotNull(recipe);

        var duplicateResponse = await client.PostAsJsonAsync($"/api/recipes/{recipe.Id}/duplicate", new { });
        await AssertStatusCodeAsync(HttpStatusCode.Created, duplicateResponse);
        var duplicated = await duplicateResponse.Content.ReadFromJsonAsync<IdPayload>(JsonOptions);
        Assert.NotNull(duplicated);
        Assert.NotEqual(recipe.Id, duplicated.Id);

        var deleteOriginal = await client.DeleteAsync($"/api/recipes/{recipe.Id}");
        await AssertStatusCodeAsync(HttpStatusCode.NoContent, deleteOriginal);

        var getDuplicate = await client.GetAsync($"/api/recipes/{duplicated.Id}");
        await AssertStatusCodeAsync(HttpStatusCode.OK, getDuplicate);
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync() {
        var client = factory.CreateClient();
        var email = $"flow-tests-{Guid.NewGuid():N}@example.com";
        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterHttpRequest(email, "Password123!", "en"));
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<AuthPayload>(JsonOptions);
        Assert.NotNull(payload);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", payload.AccessToken);
        return client;
    }

    private static async Task AssertStatusCodeAsync(HttpStatusCode expected, HttpResponseMessage response) {
        if (response.StatusCode == expected) return;
        var body = await response.Content.ReadAsStringAsync();
        Assert.Fail($"Expected {(int)expected} ({expected}), got {(int)response.StatusCode} ({response.StatusCode}). Body: {body}");
    }

    private sealed record AuthPayload(string AccessToken);
    private sealed record IdPayload(Guid Id);
    private sealed record ErrorPayload(string Error, string Message);
    private sealed record DashboardPayload(DashboardStatisticsPayload Statistics);
    private sealed record DashboardStatisticsPayload(double TotalCalories);
    private sealed record HydrationDailyTotalPayload(int TotalMl);
}
