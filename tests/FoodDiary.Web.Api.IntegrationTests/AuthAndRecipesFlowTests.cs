using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FoodDiary.Presentation.Api.Features.Auth.Requests;
using FoodDiary.Presentation.Api.Features.Consumptions.Requests;
using FoodDiary.Presentation.Api.Features.FavoriteRecipes.Requests;
using FoodDiary.Presentation.Api.Features.Products.Requests;
using FoodDiary.Presentation.Api.Features.Recipes.Requests;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

namespace FoodDiary.Web.Api.IntegrationTests;

public sealed class AuthAndRecipesFlowTests(ApiWebApplicationFactory factory)
    : IClassFixture<ApiWebApplicationFactory> {
    private static readonly JsonSerializerOptions JsonOptions = new() {
        PropertyNameCaseInsensitive = true,
    };

    [Fact]
    public async Task RecipesController_RequiresAuthentication() {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/recipes");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateRecipe_ReturnsCreatedAndLocationHeader() {
        var client = await CreateAuthenticatedClientAsync();
        var productId = await CreateProductAsync(client, "Recipe Ingredient");

        var response = await client.PostAsJsonAsync(
            "/api/v1/recipes",
            new CreateRecipeHttpRequest(
                "Created Recipe",
                "Contract recipe",
                null,
                "Dinner",
                null,
                null,
                10,
                20,
                2,
                "Private",
                true,
                null,
                null,
                null,
                null,
                null,
                null,
                [
                    new RecipeStepHttpRequest(
                        "Cook",
                        "Boil ingredients",
                        [new RecipeIngredientHttpRequest(productId, null, 200)],
                        null,
                        null)
                ]));

        var payload = await response.Content.ReadFromJsonAsync<RecipePayload>(JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(payload);
        Assert.NotNull(response.Headers.Location);
        Assert.EndsWith($"/api/v1/Recipes/{payload.Id}", response.Headers.Location.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RecipesOverview_AndRecent_ReturnFavoritePreviewRecentItemsAndFavoriteFlags() {
        var client = await CreateAuthenticatedClientAsync();
        var ingredientId = await CreateProductAsync(client, "Overview Ingredient");
        var firstRecipeId = await CreateRecipeAsync(client, ingredientId, "Overview Salad");
        var favoriteRecipeId = await CreateRecipeAsync(client, ingredientId, "Overview Soup");

        var favoriteResponse = await client.PostAsJsonAsync(
            "/api/v1/favorite-recipes",
            new AddFavoriteRecipeHttpRequest(favoriteRecipeId, "Favorite soup"));
        favoriteResponse.EnsureSuccessStatusCode();

        var consumptionResponse = await client.PostAsJsonAsync(
            "/api/v1/consumptions",
            new CreateConsumptionHttpRequest(
                DateTime.UtcNow.Date,
                "Dinner",
                null,
                null,
                null,
                [new ConsumptionItemHttpRequest(null, favoriteRecipeId, 250)]));
        consumptionResponse.EnsureSuccessStatusCode();

        var overviewResponse = await client.GetAsync("/api/v1/recipes/overview?page=1&limit=10&includePublic=true&recentLimit=10&favoriteLimit=10");
        var recentResponse = await client.GetAsync("/api/v1/recipes/recent?limit=10&includePublic=true");

        overviewResponse.EnsureSuccessStatusCode();
        recentResponse.EnsureSuccessStatusCode();

        using var overviewJson = JsonDocument.Parse(await overviewResponse.Content.ReadAsStringAsync());
        using var recentJson = JsonDocument.Parse(await recentResponse.Content.ReadAsStringAsync());

        var overviewRoot = overviewJson.RootElement;
        var recentItems = recentJson.RootElement;
        var favoriteItems = overviewRoot.GetProperty("favoriteItems");
        var recentOverviewItems = overviewRoot.GetProperty("recentItems");
        var allRecipes = overviewRoot.GetProperty("allRecipes").GetProperty("data");

        Assert.Equal(1, overviewRoot.GetProperty("favoriteTotalCount").GetInt32());
        Assert.Contains(favoriteItems.EnumerateArray(), item => item.GetProperty("recipeId").GetGuid() == favoriteRecipeId);
        Assert.Contains(recentOverviewItems.EnumerateArray(), item => item.GetProperty("id").GetGuid() == favoriteRecipeId);
        Assert.Contains(recentItems.EnumerateArray(), item => item.GetProperty("id").GetGuid() == favoriteRecipeId);

        var favoriteRecipe = allRecipes.EnumerateArray().Single(item => item.GetProperty("id").GetGuid() == favoriteRecipeId);
        var nonFavoriteRecipe = allRecipes.EnumerateArray().Single(item => item.GetProperty("id").GetGuid() == firstRecipeId);
        Assert.True(favoriteRecipe.GetProperty("isFavorite").GetBoolean());
        Assert.NotEqual(Guid.Empty, favoriteRecipe.GetProperty("favoriteRecipeId").GetGuid());
        Assert.False(nonFavoriteRecipe.GetProperty("isFavorite").GetBoolean());
    }

    [Fact]
    public async Task UpdateRecipe_PersistsPatchedValues() {
        var client = await CreateAuthenticatedClientAsync();
        var ingredientId = await CreateProductAsync(client, "Update Ingredient");
        var recipeId = await CreateRecipeAsync(client, ingredientId, "Patchable Recipe");

        var updateResponse = await client.PatchAsJsonAsync(
            $"/api/v1/recipes/{recipeId}",
            new UpdateRecipeHttpRequest(
                Name: "Updated Recipe",
                Description: "Updated description",
                ClearDescription: false,
                Comment: "Updated comment",
                ClearComment: false,
                Category: "Lunch",
                ClearCategory: false,
                ImageUrl: null,
                ClearImageUrl: false,
                ImageAssetId: null,
                ClearImageAssetId: false,
                PrepTime: 12,
                CookTime: 28,
                Servings: 3,
                Visibility: "Private",
                CalculateNutritionAutomatically: true,
                ManualCalories: null,
                ManualProteins: null,
                ManualFats: null,
                ManualCarbs: null,
                ManualFiber: null,
                ManualAlcohol: null,
                Steps: [
                    new RecipeStepHttpRequest(
                        "Update",
                        "Updated step",
                        [new RecipeIngredientHttpRequest(ingredientId, null, 180)],
                        null,
                        null)
                ]));
        updateResponse.EnsureSuccessStatusCode();

        var getResponse = await client.GetAsync($"/api/v1/recipes/{recipeId}");
        getResponse.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await getResponse.Content.ReadAsStringAsync());

        Assert.Equal("Updated Recipe", json.RootElement.GetProperty("name").GetString());
        Assert.Equal("Updated description", json.RootElement.GetProperty("description").GetString());
        Assert.Equal("Updated comment", json.RootElement.GetProperty("comment").GetString());
        Assert.Equal(3, json.RootElement.GetProperty("servings").GetInt32());
    }

    [Fact]
    public async Task DuplicateRecipe_ReturnsIndependentCopy() {
        var client = await CreateAuthenticatedClientAsync();
        var ingredientId = await CreateProductAsync(client, "Duplicate Ingredient");
        var originalId = await CreateRecipeAsync(client, ingredientId, "Original Recipe");

        var duplicateResponse = await client.PostAsJsonAsync($"/api/v1/recipes/{originalId}/duplicate", new { });
        duplicateResponse.EnsureSuccessStatusCode();
        var duplicate = await duplicateResponse.Content.ReadFromJsonAsync<RecipePayload>(JsonOptions);

        Assert.NotNull(duplicate);
        Assert.NotEqual(originalId, duplicate.Id);

        var duplicateGetResponse = await client.GetAsync($"/api/v1/recipes/{duplicate.Id}");
        duplicateGetResponse.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await duplicateGetResponse.Content.ReadAsStringAsync());

        Assert.Equal("Original Recipe", json.RootElement.GetProperty("name").GetString());
    }

    [Fact]
    public async Task DeleteRecipe_RemovesItFromSubsequentRead() {
        var client = await CreateAuthenticatedClientAsync();
        var ingredientId = await CreateProductAsync(client, "Delete Ingredient");
        var recipeId = await CreateRecipeAsync(client, ingredientId, "Delete Recipe");

        var deleteResponse = await client.DeleteAsync($"/api/v1/recipes/{recipeId}");
        var getResponse = await client.GetAsync($"/api/v1/recipes/{recipeId}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync() {
        var client = factory.CreateClient();
        var email = $"api-recipe-tests-{Guid.NewGuid():N}@example.com";
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

        var payload = await response.Content.ReadFromJsonAsync<RecipePayload>(JsonOptions);
        Assert.NotNull(payload);
        return payload.Id;
    }

    private static async Task<Guid> CreateRecipeAsync(HttpClient client, Guid ingredientId, string name) {
        var response = await client.PostAsJsonAsync(
            "/api/v1/recipes",
            new CreateRecipeHttpRequest(
                name,
                "Recipe description",
                "Recipe comment",
                "Dinner",
                null,
                null,
                10,
                20,
                2,
                "Private",
                true,
                null,
                null,
                null,
                null,
                null,
                null,
                [
                    new RecipeStepHttpRequest(
                        "Cook",
                        "Cook recipe",
                        [new RecipeIngredientHttpRequest(ingredientId, null, 200)],
                        null,
                        null)
                ]));
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<RecipePayload>(JsonOptions);
        Assert.NotNull(payload);
        return payload.Id;
    }

    private sealed record AuthPayload(string AccessToken, string RefreshToken, AuthUserPayload User);
    private sealed record AuthUserPayload(Guid Id, string Email);
    private sealed record RecipePayload(Guid Id);
}
