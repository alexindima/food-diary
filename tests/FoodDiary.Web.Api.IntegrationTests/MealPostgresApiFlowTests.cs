using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FoodDiary.Presentation.Api.Features.Auth.Requests;
using FoodDiary.Presentation.Api.Features.Consumptions.Requests;
using FoodDiary.Presentation.Api.Features.Products.Requests;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class MealPostgresApiFlowTests(PostgresApiWebApplicationFactory factory)
    : IClassFixture<PostgresApiWebApplicationFactory> {
    private static readonly JsonSerializerOptions JsonOptions = new() {
        PropertyNameCaseInsensitive = true,
    };

    [RequiresDockerFact]
    public async Task CreateUpdateReadMeal_PersistsItemsAndOverviewPayloadAgainstPostgres() {
        HttpClient client = factory.CreateClient();
        string accessToken = await RegisterAndGetAccessTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        ProductPayload product = await CreateMealProductAsync(client);
        var createdExpected = new ExpectedMeal(
            Date: new DateTime(2026, 6, 19, 12, 30, 0, DateTimeKind.Utc),
            MealType: "Lunch",
            Comment: "Postgres meal flow created",
            Amount: 180,
            PreMealSatietyLevel: 2,
            PostMealSatietyLevel: 4,
            TotalCalories: 216,
            TotalProteins: 18,
            TotalFats: 9,
            TotalCarbs: 36,
            TotalFiber: 5.4,
            TotalAlcohol: 0);

        HttpResponseMessage createResponse = await client.PostAsJsonAsync(
            "/api/v1/consumptions",
            CreateMealRequest(createdExpected, product.Id));
        MealPayload? meal = await createResponse.Content.ReadFromJsonAsync<MealPayload>(JsonOptions);

        await AssertStatusCodeAsync(HttpStatusCode.Created, createResponse);
        Assert.NotNull(meal);

        await AssertMealDetailAsync(client, meal.Id, product.Id, createdExpected);
        await AssertMealListAsync(client, meal.Id, product.Id, createdExpected);
        await AssertMealOverviewAsync(client, meal.Id, product.Id, createdExpected);

        var updatedExpected = new ExpectedMeal(
            Date: new DateTime(2026, 6, 19, 19, 45, 0, DateTimeKind.Utc),
            MealType: "Dinner",
            Comment: "Postgres meal flow updated",
            Amount: 220,
            PreMealSatietyLevel: 1,
            PostMealSatietyLevel: 5,
            TotalCalories: 264,
            TotalProteins: 22,
            TotalFats: 11,
            TotalCarbs: 44,
            TotalFiber: 6.6,
            TotalAlcohol: 0);

        HttpResponseMessage updateResponse = await client.PatchAsJsonAsync(
            $"/api/v1/consumptions/{meal.Id}",
            UpdateMealRequest(updatedExpected, product.Id));

        await AssertStatusCodeAsync(HttpStatusCode.OK, updateResponse);
        await AssertMealDetailAsync(client, meal.Id, product.Id, updatedExpected);
        await AssertMealListAsync(client, meal.Id, product.Id, updatedExpected);
        await AssertMealOverviewAsync(client, meal.Id, product.Id, updatedExpected);
    }

    private static CreateConsumptionHttpRequest CreateMealRequest(ExpectedMeal expected, Guid productId) =>
        new(
            expected.Date,
            expected.MealType,
            expected.Comment,
            ImageUrl: null,
            ImageAssetId: null,
            [new ConsumptionItemHttpRequest(productId, RecipeId: null, expected.Amount)],
            AiSessions: null,
            IsNutritionAutoCalculated: true,
            ManualCalories: null,
            ManualProteins: null,
            ManualFats: null,
            ManualCarbs: null,
            ManualFiber: null,
            ManualAlcohol: null,
            expected.PreMealSatietyLevel,
            expected.PostMealSatietyLevel);

    private static UpdateConsumptionHttpRequest UpdateMealRequest(ExpectedMeal expected, Guid productId) =>
        new(
            expected.Date,
            expected.MealType,
            expected.Comment,
            ImageUrl: null,
            ImageAssetId: null,
            [new ConsumptionItemHttpRequest(productId, RecipeId: null, expected.Amount)],
            AiSessions: null,
            IsNutritionAutoCalculated: true,
            ManualCalories: null,
            ManualProteins: null,
            ManualFats: null,
            ManualCarbs: null,
            ManualFiber: null,
            ManualAlcohol: null,
            expected.PreMealSatietyLevel,
            expected.PostMealSatietyLevel);

    private static async Task AssertMealDetailAsync(HttpClient client, Guid mealId, Guid productId, ExpectedMeal expected) {
        HttpResponseMessage response = await client.GetAsync($"/api/v1/consumptions/{mealId}").ConfigureAwait(false);
        await AssertStatusCodeAsync(HttpStatusCode.OK, response).ConfigureAwait(false);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
        AssertMealJson(json.RootElement, mealId, productId, expected);
    }

    private static async Task AssertMealListAsync(HttpClient client, Guid mealId, Guid productId, ExpectedMeal expected) {
        HttpResponseMessage response = await client.GetAsync("/api/v1/consumptions?page=1&limit=10").ConfigureAwait(false);
        await AssertStatusCodeAsync(HttpStatusCode.OK, response).ConfigureAwait(false);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
        JsonElement meal = json.RootElement
            .GetProperty("data")
            .EnumerateArray()
            .Single(item => item.GetProperty("id").GetGuid() == mealId);

        AssertMealJson(meal, mealId, productId, expected);
    }

    private static async Task AssertMealOverviewAsync(HttpClient client, Guid mealId, Guid productId, ExpectedMeal expected) {
        HttpResponseMessage response = await client.GetAsync("/api/v1/consumptions/overview?page=1&limit=10&favoriteLimit=10").ConfigureAwait(false);
        await AssertStatusCodeAsync(HttpStatusCode.OK, response).ConfigureAwait(false);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
        JsonElement meal = json.RootElement
            .GetProperty("allConsumptions")
            .GetProperty("data")
            .EnumerateArray()
            .Single(item => item.GetProperty("id").GetGuid() == mealId);

        AssertMealJson(meal, mealId, productId, expected);
    }

    private static void AssertMealJson(JsonElement meal, Guid mealId, Guid productId, ExpectedMeal expected) {
        Assert.Equal(mealId, meal.GetProperty("id").GetGuid());
        Assert.Equal(expected.MealType, meal.GetProperty("mealType").GetString());
        Assert.Equal(expected.Comment, meal.GetProperty("comment").GetString());
        Assert.True(meal.GetProperty("isNutritionAutoCalculated").GetBoolean());
        Assert.Equal(expected.PreMealSatietyLevel, meal.GetProperty("preMealSatietyLevel").GetInt32());
        Assert.Equal(expected.PostMealSatietyLevel, meal.GetProperty("postMealSatietyLevel").GetInt32());
        Assert.Equal(expected.TotalCalories, meal.GetProperty("totalCalories").GetDouble(), precision: 2);
        Assert.Equal(expected.TotalProteins, meal.GetProperty("totalProteins").GetDouble(), precision: 2);
        Assert.Equal(expected.TotalFats, meal.GetProperty("totalFats").GetDouble(), precision: 2);
        Assert.Equal(expected.TotalCarbs, meal.GetProperty("totalCarbs").GetDouble(), precision: 2);
        Assert.Equal(expected.TotalFiber, meal.GetProperty("totalFiber").GetDouble(), precision: 2);
        Assert.Equal(expected.TotalAlcohol, meal.GetProperty("totalAlcohol").GetDouble(), precision: 2);

        JsonElement item = Assert.Single(meal.GetProperty("items").EnumerateArray());
        Assert.Equal(mealId, item.GetProperty("consumptionId").GetGuid());
        Assert.Equal(productId, item.GetProperty("productId").GetGuid());
        Assert.Equal(expected.Amount, item.GetProperty("amount").GetDouble(), precision: 2);
        Assert.Equal("Postgres Meal Ingredient", item.GetProperty("productName").GetString());
        Assert.Equal("G", item.GetProperty("productBaseUnit").GetString());
        Assert.Equal(100, item.GetProperty("productBaseAmount").GetDouble(), precision: 2);
        Assert.Equal(120, item.GetProperty("productCaloriesPerBase").GetDouble(), precision: 2);
        Assert.Equal(10, item.GetProperty("productProteinsPerBase").GetDouble(), precision: 2);
        Assert.Equal(5, item.GetProperty("productFatsPerBase").GetDouble(), precision: 2);
        Assert.Equal(20, item.GetProperty("productCarbsPerBase").GetDouble(), precision: 2);
        Assert.Equal(3, item.GetProperty("productFiberPerBase").GetDouble(), precision: 2);
        Assert.Equal(0, item.GetProperty("productAlcoholPerBase").GetDouble(), precision: 2);
        Assert.Equal("Manual", item.GetProperty("origin").GetString());
    }

    private static async Task<ProductPayload> CreateMealProductAsync(HttpClient client) {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/products",
            new CreateProductHttpRequest(
                Barcode: null,
                "Postgres Meal Ingredient",
                Brand: "Meal Test",
                "Grain",
                Category: "Pantry",
                Description: "Ingredient for meal flow",
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

        await AssertStatusCodeAsync(HttpStatusCode.Created, response).ConfigureAwait(false);
        ProductPayload? payload = await response.Content.ReadFromJsonAsync<ProductPayload>(JsonOptions).ConfigureAwait(false);
        Assert.NotNull(payload);
        return payload;
    }

    private static async Task<string> RegisterAndGetAccessTokenAsync(HttpClient client) {
        string email = $"postgres-meal-tests-{Guid.NewGuid():N}@example.com";
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterHttpRequest(email, "Password123!", "en")).ConfigureAwait(false);

        AuthPayload? payload = await response.Content.ReadFromJsonAsync<AuthPayload>(JsonOptions).ConfigureAwait(false);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload.AccessToken));
        return payload.AccessToken;
    }

    private static async Task AssertStatusCodeAsync(HttpStatusCode expected, HttpResponseMessage response) {
        if (response.StatusCode == expected) {
            return;
        }

        string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        Assert.Fail(
            string.Create(CultureInfo.InvariantCulture, $"Expected status {(int)expected} ({expected}), got {(int)response.StatusCode} ({response.StatusCode}). Body: {body}"));
    }

    [ExcludeFromCodeCoverage]
    private sealed record AuthPayload(string AccessToken, string RefreshToken, AuthUserPayload User);

    [ExcludeFromCodeCoverage]
    private sealed record AuthUserPayload(string Email);

    [ExcludeFromCodeCoverage]
    private sealed record ProductPayload(Guid Id);

    [ExcludeFromCodeCoverage]
    private sealed record MealPayload(Guid Id);

    [ExcludeFromCodeCoverage]
    private sealed record ExpectedMeal(
        DateTime Date,
        string MealType,
        string Comment,
        double Amount,
        int PreMealSatietyLevel,
        int PostMealSatietyLevel,
        double TotalCalories,
        double TotalProteins,
        double TotalFats,
        double TotalCarbs,
        double TotalFiber,
        double TotalAlcohol);
}
