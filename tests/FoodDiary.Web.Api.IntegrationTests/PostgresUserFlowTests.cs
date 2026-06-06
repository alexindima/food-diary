using System.Globalization;
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

[ExcludeFromCodeCoverage]
public sealed class PostgresUserFlowTests(PostgresApiWebApplicationFactory factory)
    : IClassFixture<PostgresApiWebApplicationFactory> {
    private static readonly JsonSerializerOptions JsonOptions = new() {
        PropertyNameCaseInsensitive = true,
    };

    [RequiresDockerFact]
    public async Task CreateProduct_ThenCreateConsumption_ThenGetDashboard_ReturnsNutritionData() {
        HttpClient client = await CreateAuthenticatedClientAsync();

        HttpResponseMessage createProductResponse = await client.PostAsJsonAsync("/api/v1/products", new CreateProductHttpRequest(
            Barcode: null, "Integration Apple", Brand: null, "Unknown", Category: null, Description: null, Comment: null, ImageUrl: null, ImageAssetId: null,
            "G", 100, 100, 52, 0.3, 0.2, 14, 2.4, 0, "Private"));
        await AssertStatusCodeAsync(HttpStatusCode.Created, createProductResponse);
        IdPayload? product = await createProductResponse.Content.ReadFromJsonAsync<IdPayload>(JsonOptions);
        Assert.NotNull(product);

        DateTime today = DateTime.UtcNow.Date;
        HttpResponseMessage createConsumptionResponse = await client.PostAsJsonAsync("/api/v1/consumptions", new CreateConsumptionHttpRequest(
            today, "Lunch", Comment: null, ImageUrl: null, ImageAssetId: null,
            [new ConsumptionItemHttpRequest(product.Id, RecipeId: null, 200)],
            IsNutritionAutoCalculated: true));
        await AssertStatusCodeAsync(HttpStatusCode.Created, createConsumptionResponse);

        HttpResponseMessage dashboardResponse = await client.GetAsync(string.Create(CultureInfo.InvariantCulture, $"/api/v1/dashboard?date={today:yyyy-MM-dd}"));
        await AssertStatusCodeAsync(HttpStatusCode.OK, dashboardResponse);
        DashboardPayload? dashboard = await dashboardResponse.Content.ReadFromJsonAsync<DashboardPayload>(JsonOptions);

        Assert.NotNull(dashboard);
        Assert.True(dashboard.Statistics.TotalCalories > 0, "Dashboard should show calories from the consumption");
    }

    [RequiresDockerFact]
    public async Task CreateHydrationEntries_ThenGetDailyTotal_ReturnsAggregatedAmount() {
        HttpClient client = await CreateAuthenticatedClientAsync();
        DateTime now = DateTime.UtcNow.Date.AddHours(12);

        HttpResponseMessage response1 = await client.PostAsJsonAsync("/api/v1/hydrations", new CreateHydrationEntryHttpRequest(now, 250));
        HttpResponseMessage response2 = await client.PostAsJsonAsync("/api/v1/hydrations", new CreateHydrationEntryHttpRequest(now.AddMinutes(30), 500));
        await AssertStatusCodeAsync(HttpStatusCode.OK, response1);
        await AssertStatusCodeAsync(HttpStatusCode.OK, response2);

        HttpResponseMessage totalResponse = await client.GetAsync(string.Create(CultureInfo.InvariantCulture, $"/api/v1/hydrations/daily?date={now:yyyy-MM-dd}"));
        await AssertStatusCodeAsync(HttpStatusCode.OK, totalResponse);
        HydrationDailyTotalPayload? total = await totalResponse.Content.ReadFromJsonAsync<HydrationDailyTotalPayload>(JsonOptions);

        Assert.NotNull(total);
        Assert.True(total.TotalMl >= 750, string.Create(CultureInfo.InvariantCulture, $"Expected at least 750ml, got {total.TotalMl}"));
    }

    [RequiresDockerFact]
    public async Task CreateWaistEntry_WithDuplicateDate_ReturnsConflict() {
        HttpClient client = await CreateAuthenticatedClientAsync();
        var request = new CreateWaistEntryHttpRequest(
            new DateTime(2026, 3, 27, 0, 0, 0, DateTimeKind.Utc), 80.0);

        HttpResponseMessage first = await client.PostAsJsonAsync("/api/v1/waist-entries", request);
        HttpResponseMessage duplicate = await client.PostAsJsonAsync("/api/v1/waist-entries", request);
        ErrorPayload? payload = await duplicate.Content.ReadFromJsonAsync<ErrorPayload>(JsonOptions);

        await AssertStatusCodeAsync(HttpStatusCode.OK, first);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("WaistEntry.AlreadyExists", payload.Error);
    }

    [RequiresDockerFact]
    public async Task CreateRecipe_ThenDuplicate_CreatesTwoIndependentRecipes() {
        HttpClient client = await CreateAuthenticatedClientAsync();

        HttpResponseMessage createProductResponse = await client.PostAsJsonAsync("/api/v1/products", new CreateProductHttpRequest(
            Barcode: null, "Recipe Ingredient", Brand: null, "Unknown", Category: null, Description: null, Comment: null, ImageUrl: null, ImageAssetId: null,
            "G", 100, 100, 100, 5, 3, 15, 1, 0, "Private"));
        await AssertStatusCodeAsync(HttpStatusCode.Created, createProductResponse);
        IdPayload? product = await createProductResponse.Content.ReadFromJsonAsync<IdPayload>(JsonOptions);
        Assert.NotNull(product);

        HttpResponseMessage createRecipeResponse = await client.PostAsJsonAsync("/api/v1/recipes", new CreateRecipeHttpRequest(
            "Original Soup", "Test recipe", Comment: null, "Dinner", ImageUrl: null, ImageAssetId: null,
            15, 30, 2, "private", CalculateNutritionAutomatically: true, ManualCalories: null, ManualProteins: null, ManualFats: null, ManualCarbs: null, ManualFiber: null, ManualAlcohol: null,
            [new RecipeStepHttpRequest("Boil", "Boil water",
                [new RecipeIngredientHttpRequest(product.Id, NestedRecipeId: null, 200)], ImageUrl: null, ImageAssetId: null)]));
        await AssertStatusCodeAsync(HttpStatusCode.Created, createRecipeResponse);
        IdPayload? recipe = await createRecipeResponse.Content.ReadFromJsonAsync<IdPayload>(JsonOptions);
        Assert.NotNull(recipe);

        HttpResponseMessage duplicateResponse = await client.PostAsJsonAsync(string.Create(CultureInfo.InvariantCulture, $"/api/v1/recipes/{recipe.Id}/duplicate"), new { });
        await AssertStatusCodeAsync(HttpStatusCode.OK, duplicateResponse);
        IdPayload? duplicated = await duplicateResponse.Content.ReadFromJsonAsync<IdPayload>(JsonOptions);
        Assert.NotNull(duplicated);
        Assert.NotEqual(recipe.Id, duplicated.Id);

        HttpResponseMessage deleteOriginal = await client.DeleteAsync(string.Create(CultureInfo.InvariantCulture, $"/api/v1/recipes/{recipe.Id}"));
        await AssertStatusCodeAsync(HttpStatusCode.NoContent, deleteOriginal);

        HttpResponseMessage getDuplicate = await client.GetAsync(string.Create(CultureInfo.InvariantCulture, $"/api/v1/recipes/{duplicated.Id}"));
        await AssertStatusCodeAsync(HttpStatusCode.OK, getDuplicate);
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync() {
        HttpClient client = factory.CreateClient();
        string email = $"flow-tests-{Guid.NewGuid():N}@example.com";
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterHttpRequest(email, "Password123!", "en")).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        AuthPayload? payload = await response.Content.ReadFromJsonAsync<AuthPayload>(JsonOptions).ConfigureAwait(false);
        Assert.NotNull(payload);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", payload.AccessToken);
        return client;
    }

    private static async Task AssertStatusCodeAsync(HttpStatusCode expected, HttpResponseMessage response) {
        if (response.StatusCode == expected) {
            return;
        }

        string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        Assert.Fail(string.Create(CultureInfo.InvariantCulture, $"Expected {(int)expected} ({expected}), got {(int)response.StatusCode} ({response.StatusCode}). Body: {body}"));
    }

    [ExcludeFromCodeCoverage]
    private sealed record AuthPayload(string AccessToken);
    [ExcludeFromCodeCoverage]
    private sealed record IdPayload(Guid Id);
    [ExcludeFromCodeCoverage]
    private sealed record ErrorPayload(string Error, string Message);
    [ExcludeFromCodeCoverage]
    private sealed record DashboardPayload(DashboardStatisticsPayload Statistics);
    [ExcludeFromCodeCoverage]
    private sealed record DashboardStatisticsPayload(double TotalCalories);
    [ExcludeFromCodeCoverage]
    private sealed record HydrationDailyTotalPayload(int TotalMl);
}
