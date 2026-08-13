using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FoodDiary.Presentation.Api.Features.Auth.Requests;
using FoodDiary.Presentation.Api.Features.Meals.Requests;
using FoodDiary.Presentation.Api.Features.FavoriteMeals.Requests;
using FoodDiary.Presentation.Api.Features.Products.Requests;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class AuthAndMealsFlowTests(ApiWebApplicationFactory factory)
    : IClassFixture<ApiWebApplicationFactory> {
    private static readonly JsonSerializerOptions JsonOptions = new() {
        PropertyNameCaseInsensitive = true,
    };

    [Fact]
    public async Task MealsController_RequiresAuthentication() {
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/v1/meals");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateMeal_ReturnsCreatedAndLocationHeader() {
        HttpClient client = await CreateAuthenticatedClientAsync();
        Guid productId = await CreateProductAsync(client, "Meal Ingredient");

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/meals",
            new CreateMealHttpRequest(
                new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc),
                "Lunch",
                "Created meal",
                ImageUrl: null,
                ImageAssetId: null,
                [new MealItemHttpRequest(productId, RecipeId: null, 180)]));

        MealPayload? payload = await response.Content.ReadFromJsonAsync<MealPayload>(JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(payload);
        Assert.NotNull(response.Headers.Location);
        Assert.EndsWith($"/api/v1/Meals/{payload.Id}", response.Headers.Location.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MealsOverview_ReturnsFavoritePreviewAndFavoriteFlags() {
        HttpClient client = await CreateAuthenticatedClientAsync();
        Guid productId = await CreateProductAsync(client, "Overview Ingredient");
        Guid firstMealId = await CreateMealAsync(client, productId, "Breakfast", "Overview breakfast");
        Guid favoriteMealId = await CreateMealAsync(client, productId, "Dinner", "Overview dinner");

        HttpResponseMessage favoriteResponse = await client.PostAsJsonAsync(
            "/api/v1/favorite-meals",
            new AddFavoriteMealHttpRequest(favoriteMealId, "Favorite dinner"));
        favoriteResponse.EnsureSuccessStatusCode();

        HttpResponseMessage overviewResponse = await client.GetAsync("/api/v1/meals/overview?page=1&limit=10&favoriteLimit=10");
        overviewResponse.EnsureSuccessStatusCode();

        using var overviewJson = JsonDocument.Parse(await overviewResponse.Content.ReadAsStringAsync());
        JsonElement overviewRoot = overviewJson.RootElement;
        JsonElement favoriteItems = overviewRoot.GetProperty("favoriteItems");
        JsonElement allMeals = overviewRoot.GetProperty("allMeals").GetProperty("data");

        Assert.Equal(1, overviewRoot.GetProperty("favoriteTotalCount").GetInt32());
        Assert.Contains(favoriteItems.EnumerateArray(), item => item.GetProperty("mealId").GetGuid() == favoriteMealId);

        JsonElement favoriteMeal = allMeals.EnumerateArray().Single(item => item.GetProperty("id").GetGuid() == favoriteMealId);
        JsonElement nonFavoriteMeal = allMeals.EnumerateArray().Single(item => item.GetProperty("id").GetGuid() == firstMealId);
        Assert.True(favoriteMeal.GetProperty("isFavorite").GetBoolean());
        Assert.NotEqual(Guid.Empty, favoriteMeal.GetProperty("favoriteMealId").GetGuid());
        Assert.False(nonFavoriteMeal.GetProperty("isFavorite").GetBoolean());
    }

    [Fact]
    public async Task UpdateMeal_PersistsPatchedValues() {
        HttpClient client = await CreateAuthenticatedClientAsync();
        Guid productId = await CreateProductAsync(client, "Update Ingredient");
        Guid mealId = await CreateMealAsync(client, productId, "Lunch", "Patchable meal");

        HttpResponseMessage updateResponse = await client.PatchAsJsonAsync(
            $"/api/v1/meals/{mealId}",
            new UpdateMealHttpRequest(
                new DateTime(2026, 3, 26, 19, 0, 0, DateTimeKind.Utc),
                "Dinner",
                "Updated meal",
                ImageUrl: null,
                ImageAssetId: null,
                [new MealItemHttpRequest(productId, RecipeId: null, 220)],
                IsNutritionAutoCalculated: true,
                PreMealSatietyLevel: 2,
                PostMealSatietyLevel: 4));
        updateResponse.EnsureSuccessStatusCode();

        HttpResponseMessage getResponse = await client.GetAsync($"/api/v1/meals/{mealId}");
        getResponse.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await getResponse.Content.ReadAsStringAsync());

        Assert.Equal("Dinner", json.RootElement.GetProperty("mealType").GetString());
        Assert.Equal("Updated meal", json.RootElement.GetProperty("comment").GetString());
        Assert.Equal(2, json.RootElement.GetProperty("preMealSatietyLevel").GetInt32());
        Assert.Equal(4, json.RootElement.GetProperty("postMealSatietyLevel").GetInt32());
    }

    [Fact]
    public async Task RepeatMeal_ReturnsNewMealCopy() {
        HttpClient client = await CreateAuthenticatedClientAsync();
        Guid productId = await CreateProductAsync(client, "Repeat Ingredient");
        Guid sourceMealId = await CreateMealAsync(client, productId, "Lunch", "Repeat source");

        HttpResponseMessage repeatResponse = await client.PostAsJsonAsync(
            $"/api/v1/meals/{sourceMealId}/repeat",
            new RepeatMealHttpRequest(new DateTime(2026, 3, 27, 0, 0, 0, DateTimeKind.Utc), "Dinner"));
        repeatResponse.EnsureSuccessStatusCode();
        MealPayload? repeated = await repeatResponse.Content.ReadFromJsonAsync<MealPayload>(JsonOptions);

        Assert.NotNull(repeated);
        Assert.NotEqual(sourceMealId, repeated.Id);

        HttpResponseMessage getResponse = await client.GetAsync($"/api/v1/meals/{repeated.Id}");
        getResponse.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await getResponse.Content.ReadAsStringAsync());

        Assert.Equal("Dinner", json.RootElement.GetProperty("mealType").GetString());
        Assert.Single(json.RootElement.GetProperty("items").EnumerateArray());
    }

    [Fact]
    public async Task DeleteMeal_RemovesItFromSubsequentRead() {
        HttpClient client = await CreateAuthenticatedClientAsync();
        Guid productId = await CreateProductAsync(client, "Delete Ingredient");
        Guid mealId = await CreateMealAsync(client, productId, "Lunch", "Delete meal");

        HttpResponseMessage deleteResponse = await client.DeleteAsync($"/api/v1/meals/{mealId}");
        HttpResponseMessage getResponse = await client.GetAsync($"/api/v1/meals/{mealId}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync() {
        HttpClient client = factory.CreateClient();
        string email = $"api-meal-tests-{Guid.NewGuid():N}@example.com";
        HttpResponseMessage registerResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterHttpRequest(email, "Password123!", "en")).ConfigureAwait(false);
        registerResponse.EnsureSuccessStatusCode();

        AuthPayload? authPayload = await registerResponse.Content.ReadFromJsonAsync<AuthPayload>(JsonOptions).ConfigureAwait(false);
        Assert.NotNull(authPayload);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authPayload.AccessToken);
        return client;
    }

    private static async Task<Guid> CreateProductAsync(HttpClient client, string name) {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/products",
            new CreateProductHttpRequest(
                Barcode: null,
                name,
                Brand: null,
                "Unknown",
                Category: null,
                Description: null,
                Comment: null,
                ImageUrl: null,
                ImageAssetId: null,
                "G",
                100,
                100,
                120,
                10,
                5,
                20,
                3,
                0,
                "Private")).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        MealPayload? payload = await response.Content.ReadFromJsonAsync<MealPayload>(JsonOptions).ConfigureAwait(false);
        Assert.NotNull(payload);
        return payload.Id;
    }

    private static async Task<Guid> CreateMealAsync(HttpClient client, Guid productId, string mealType, string comment) {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/meals",
            new CreateMealHttpRequest(
                new DateTime(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc),
                mealType,
                comment,
                ImageUrl: null,
                ImageAssetId: null,
                [new MealItemHttpRequest(productId, RecipeId: null, 180)])).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        MealPayload? payload = await response.Content.ReadFromJsonAsync<MealPayload>(JsonOptions).ConfigureAwait(false);
        Assert.NotNull(payload);
        return payload.Id;
    }

    [ExcludeFromCodeCoverage]
    private sealed record AuthPayload(string AccessToken, string RefreshToken, AuthUserPayload User);
    [ExcludeFromCodeCoverage]
    private sealed record AuthUserPayload(Guid Id, string Email);
    [ExcludeFromCodeCoverage]
    private sealed record MealPayload(Guid Id);
}
