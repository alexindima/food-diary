using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FoodDiary.Presentation.Api.Features.Auth.Requests;
using FoodDiary.Presentation.Api.Features.Consumptions.Requests;
using FoodDiary.Presentation.Api.Features.FavoriteMeals.Requests;
using FoodDiary.Presentation.Api.Features.Products.Requests;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

namespace FoodDiary.Web.Api.IntegrationTests;

public sealed class AuthAndConsumptionsFlowTests(ApiWebApplicationFactory factory)
    : IClassFixture<ApiWebApplicationFactory> {
    private static readonly JsonSerializerOptions JsonOptions = new() {
        PropertyNameCaseInsensitive = true,
    };

    [Fact]
    public async Task ConsumptionsController_RequiresAuthentication() {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/consumptions");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateConsumption_ReturnsCreatedAndLocationHeader() {
        var client = await CreateAuthenticatedClientAsync();
        var productId = await CreateProductAsync(client, "Consumption Ingredient");

        var response = await client.PostAsJsonAsync(
            "/api/v1/consumptions",
            new CreateConsumptionHttpRequest(
                new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc),
                "Lunch",
                "Created meal",
                null,
                null,
                [new ConsumptionItemHttpRequest(productId, null, 180)]));

        var payload = await response.Content.ReadFromJsonAsync<ConsumptionPayload>(JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(payload);
        Assert.NotNull(response.Headers.Location);
        Assert.EndsWith($"/api/v1/Consumptions/{payload.Id}", response.Headers.Location.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ConsumptionsOverview_ReturnsFavoritePreviewAndFavoriteFlags() {
        var client = await CreateAuthenticatedClientAsync();
        var productId = await CreateProductAsync(client, "Overview Ingredient");
        var firstMealId = await CreateConsumptionAsync(client, productId, "Breakfast", "Overview breakfast");
        var favoriteMealId = await CreateConsumptionAsync(client, productId, "Dinner", "Overview dinner");

        var favoriteResponse = await client.PostAsJsonAsync(
            "/api/v1/favorite-meals",
            new AddFavoriteMealHttpRequest(favoriteMealId, "Favorite dinner"));
        favoriteResponse.EnsureSuccessStatusCode();

        var overviewResponse = await client.GetAsync("/api/v1/consumptions/overview?page=1&limit=10&favoriteLimit=10");
        overviewResponse.EnsureSuccessStatusCode();

        using var overviewJson = JsonDocument.Parse(await overviewResponse.Content.ReadAsStringAsync());
        var overviewRoot = overviewJson.RootElement;
        var favoriteItems = overviewRoot.GetProperty("favoriteItems");
        var allConsumptions = overviewRoot.GetProperty("allConsumptions").GetProperty("data");

        Assert.Equal(1, overviewRoot.GetProperty("favoriteTotalCount").GetInt32());
        Assert.Contains(favoriteItems.EnumerateArray(), item => item.GetProperty("mealId").GetGuid() == favoriteMealId);

        var favoriteMeal = allConsumptions.EnumerateArray().Single(item => item.GetProperty("id").GetGuid() == favoriteMealId);
        var nonFavoriteMeal = allConsumptions.EnumerateArray().Single(item => item.GetProperty("id").GetGuid() == firstMealId);
        Assert.True(favoriteMeal.GetProperty("isFavorite").GetBoolean());
        Assert.NotEqual(Guid.Empty, favoriteMeal.GetProperty("favoriteMealId").GetGuid());
        Assert.False(nonFavoriteMeal.GetProperty("isFavorite").GetBoolean());
    }

    [Fact]
    public async Task UpdateConsumption_PersistsPatchedValues() {
        var client = await CreateAuthenticatedClientAsync();
        var productId = await CreateProductAsync(client, "Update Ingredient");
        var mealId = await CreateConsumptionAsync(client, productId, "Lunch", "Patchable meal");

        var updateResponse = await client.PatchAsJsonAsync(
            $"/api/v1/consumptions/{mealId}",
            new UpdateConsumptionHttpRequest(
                new DateTime(2026, 3, 26, 19, 0, 0, DateTimeKind.Utc),
                "Dinner",
                "Updated meal",
                null,
                null,
                [new ConsumptionItemHttpRequest(productId, null, 220)],
                IsNutritionAutoCalculated: true,
                PreMealSatietyLevel: 2,
                PostMealSatietyLevel: 4));
        updateResponse.EnsureSuccessStatusCode();

        var getResponse = await client.GetAsync($"/api/v1/consumptions/{mealId}");
        getResponse.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await getResponse.Content.ReadAsStringAsync());

        Assert.Equal("Dinner", json.RootElement.GetProperty("mealType").GetString());
        Assert.Equal("Updated meal", json.RootElement.GetProperty("comment").GetString());
        Assert.Equal(2, json.RootElement.GetProperty("preMealSatietyLevel").GetInt32());
        Assert.Equal(4, json.RootElement.GetProperty("postMealSatietyLevel").GetInt32());
    }

    [Fact]
    public async Task RepeatConsumption_ReturnsNewMealCopy() {
        var client = await CreateAuthenticatedClientAsync();
        var productId = await CreateProductAsync(client, "Repeat Ingredient");
        var sourceMealId = await CreateConsumptionAsync(client, productId, "Lunch", "Repeat source");

        var repeatResponse = await client.PostAsJsonAsync(
            $"/api/v1/consumptions/{sourceMealId}/repeat",
            new RepeatMealHttpRequest(new DateTime(2026, 3, 27, 0, 0, 0, DateTimeKind.Utc), "Dinner"));
        repeatResponse.EnsureSuccessStatusCode();
        var repeated = await repeatResponse.Content.ReadFromJsonAsync<ConsumptionPayload>(JsonOptions);

        Assert.NotNull(repeated);
        Assert.NotEqual(sourceMealId, repeated.Id);

        var getResponse = await client.GetAsync($"/api/v1/consumptions/{repeated.Id}");
        getResponse.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await getResponse.Content.ReadAsStringAsync());

        Assert.Equal("Dinner", json.RootElement.GetProperty("mealType").GetString());
        Assert.Single(json.RootElement.GetProperty("items").EnumerateArray());
    }

    [Fact]
    public async Task DeleteConsumption_RemovesItFromSubsequentRead() {
        var client = await CreateAuthenticatedClientAsync();
        var productId = await CreateProductAsync(client, "Delete Ingredient");
        var mealId = await CreateConsumptionAsync(client, productId, "Lunch", "Delete meal");

        var deleteResponse = await client.DeleteAsync($"/api/v1/consumptions/{mealId}");
        var getResponse = await client.GetAsync($"/api/v1/consumptions/{mealId}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync() {
        var client = factory.CreateClient();
        var email = $"api-consumption-tests-{Guid.NewGuid():N}@example.com";
        var registerResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterHttpRequest(email, "Password123!", "en"));
        registerResponse.EnsureSuccessStatusCode();

        var authPayload = await registerResponse.Content.ReadFromJsonAsync<AuthPayload>(JsonOptions);
        Assert.NotNull(authPayload);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authPayload.AccessToken);
        return client;
    }

    private static async Task<Guid> CreateProductAsync(HttpClient client, string name) {
        var response = await client.PostAsJsonAsync(
            "/api/v1/products",
            new CreateProductHttpRequest(
                null,
                name,
                null,
                "Unknown",
                null,
                null,
                null,
                null,
                null,
                "G",
                100,
                100,
                120,
                10,
                5,
                20,
                3,
                0,
                "Private"));
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<ConsumptionPayload>(JsonOptions);
        Assert.NotNull(payload);
        return payload.Id;
    }

    private static async Task<Guid> CreateConsumptionAsync(HttpClient client, Guid productId, string mealType, string comment) {
        var response = await client.PostAsJsonAsync(
            "/api/v1/consumptions",
            new CreateConsumptionHttpRequest(
                new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc),
                mealType,
                comment,
                null,
                null,
                [new ConsumptionItemHttpRequest(productId, null, 180)]));
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<ConsumptionPayload>(JsonOptions);
        Assert.NotNull(payload);
        return payload.Id;
    }

    private sealed record AuthPayload(string AccessToken, string RefreshToken, AuthUserPayload User);
    private sealed record AuthUserPayload(Guid Id, string Email);
    private sealed record ConsumptionPayload(Guid Id);
}
