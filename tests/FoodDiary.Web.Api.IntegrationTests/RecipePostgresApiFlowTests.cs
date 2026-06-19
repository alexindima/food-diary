using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FoodDiary.Presentation.Api.Features.Auth.Requests;
using FoodDiary.Presentation.Api.Features.Products.Requests;
using FoodDiary.Presentation.Api.Features.Recipes.Requests;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class RecipePostgresApiFlowTests(PostgresApiWebApplicationFactory factory)
    : IClassFixture<PostgresApiWebApplicationFactory> {
    private static readonly JsonSerializerOptions JsonOptions = new() {
        PropertyNameCaseInsensitive = true,
    };

    [RequiresDockerFact]
    public async Task CreateUpdateReadRecipe_PersistsIngredientsAndOverviewPayloadAgainstPostgres() {
        HttpClient client = factory.CreateClient();
        string accessToken = await RegisterAndGetAccessTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        ProductPayload product = await CreateIngredientProductAsync(client);
        var createdExpected = new ExpectedRecipe(
            Name: "Postgres Recipe Flow",
            Description: "Created recipe description",
            Comment: "Created recipe comment",
            Category: "Dinner",
            PrepTime: 5,
            CookTime: 25,
            Servings: 2,
            Visibility: "Private",
            StepTitle: "Cook",
            StepInstruction: "Cook ingredient.",
            IngredientAmount: 150,
            TotalCalories: 300,
            TotalProteins: 15,
            TotalFats: 7.5,
            TotalCarbs: 45,
            TotalFiber: 6,
            TotalAlcohol: 0);

        HttpResponseMessage createResponse = await client.PostAsJsonAsync(
            "/api/v1/recipes",
            CreateRecipeRequest(createdExpected, product.Id));
        RecipePayload? recipe = await createResponse.Content.ReadFromJsonAsync<RecipePayload>(JsonOptions);

        await AssertStatusCodeAsync(HttpStatusCode.Created, createResponse);
        Assert.NotNull(recipe);

        await AssertRecipeDetailAsync(client, recipe.Id, product.Id, createdExpected);
        await AssertRecipeOverviewAsync(client, recipe.Id, product.Id, createdExpected);

        var updatedExpected = new ExpectedRecipe(
            Name: "Postgres Recipe Flow Updated",
            Description: "Updated recipe description",
            Comment: "Updated recipe comment",
            Category: "Lunch",
            PrepTime: 8,
            CookTime: 30,
            Servings: 3,
            Visibility: "Private",
            StepTitle: "Finish",
            StepInstruction: "Finish ingredient.",
            IngredientAmount: 200,
            TotalCalories: 400,
            TotalProteins: 20,
            TotalFats: 10,
            TotalCarbs: 60,
            TotalFiber: 8,
            TotalAlcohol: 0);

        HttpResponseMessage updateResponse = await client.PatchAsJsonAsync(
            $"/api/v1/recipes/{recipe.Id}",
            UpdateRecipeRequest(updatedExpected, product.Id));

        await AssertStatusCodeAsync(HttpStatusCode.OK, updateResponse);
        await AssertRecipeDetailAsync(client, recipe.Id, product.Id, updatedExpected);
        await AssertRecipeOverviewAsync(client, recipe.Id, product.Id, updatedExpected);
    }

    private static CreateRecipeHttpRequest CreateRecipeRequest(ExpectedRecipe expected, Guid productId) =>
        new(
            expected.Name,
            expected.Description,
            expected.Comment,
            expected.Category,
            ImageUrl: null,
            ImageAssetId: null,
            expected.PrepTime,
            expected.CookTime,
            expected.Servings,
            expected.Visibility,
            CalculateNutritionAutomatically: true,
            ManualCalories: null,
            ManualProteins: null,
            ManualFats: null,
            ManualCarbs: null,
            ManualFiber: null,
            ManualAlcohol: null,
            [
                new RecipeStepHttpRequest(
                    expected.StepTitle,
                    expected.StepInstruction,
                    [new RecipeIngredientHttpRequest(productId, NestedRecipeId: null, expected.IngredientAmount)],
                    ImageUrl: null,
                    ImageAssetId: null),
            ]);

    private static UpdateRecipeHttpRequest UpdateRecipeRequest(ExpectedRecipe expected, Guid productId) =>
        new(
            expected.Name,
            expected.Description,
            ClearDescription: false,
            expected.Comment,
            ClearComment: false,
            expected.Category,
            ClearCategory: false,
            ImageUrl: null,
            ClearImageUrl: false,
            ImageAssetId: null,
            ClearImageAssetId: false,
            expected.PrepTime,
            expected.CookTime,
            expected.Servings,
            expected.Visibility,
            CalculateNutritionAutomatically: true,
            ManualCalories: null,
            ManualProteins: null,
            ManualFats: null,
            ManualCarbs: null,
            ManualFiber: null,
            ManualAlcohol: null,
            [
                new RecipeStepHttpRequest(
                    expected.StepTitle,
                    expected.StepInstruction,
                    [new RecipeIngredientHttpRequest(productId, NestedRecipeId: null, expected.IngredientAmount)],
                    ImageUrl: null,
                    ImageAssetId: null),
            ]);

    private static async Task AssertRecipeDetailAsync(HttpClient client, Guid recipeId, Guid productId, ExpectedRecipe expected) {
        HttpResponseMessage response = await client.GetAsync($"/api/v1/recipes/{recipeId}").ConfigureAwait(false);
        await AssertStatusCodeAsync(HttpStatusCode.OK, response).ConfigureAwait(false);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
        AssertRecipeJson(json.RootElement, productId, expected);
    }

    private static async Task AssertRecipeOverviewAsync(HttpClient client, Guid recipeId, Guid productId, ExpectedRecipe expected) {
        HttpResponseMessage response = await client.GetAsync("/api/v1/recipes/overview?page=1&limit=10&includePublic=true&recentLimit=10&favoriteLimit=10").ConfigureAwait(false);
        await AssertStatusCodeAsync(HttpStatusCode.OK, response).ConfigureAwait(false);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
        JsonElement recipe = json.RootElement
            .GetProperty("allRecipes")
            .GetProperty("data")
            .EnumerateArray()
            .Single(item => item.GetProperty("id").GetGuid() == recipeId);

        AssertRecipeJson(recipe, productId, expected);
    }

    private static void AssertRecipeJson(JsonElement recipe, Guid productId, ExpectedRecipe expected) {
        Assert.Equal(expected.Name, recipe.GetProperty("name").GetString());
        Assert.Equal(expected.Description, recipe.GetProperty("description").GetString());
        Assert.Equal(expected.Comment, recipe.GetProperty("comment").GetString());
        Assert.Equal(expected.Category, recipe.GetProperty("category").GetString());
        Assert.Equal(expected.PrepTime, recipe.GetProperty("prepTime").GetInt32());
        Assert.Equal(expected.CookTime, recipe.GetProperty("cookTime").GetInt32());
        Assert.Equal(expected.Servings, recipe.GetProperty("servings").GetInt32());
        Assert.Equal(expected.Visibility, recipe.GetProperty("visibility").GetString());
        Assert.Equal(expected.TotalCalories, recipe.GetProperty("totalCalories").GetDouble(), precision: 2);
        Assert.Equal(expected.TotalProteins, recipe.GetProperty("totalProteins").GetDouble(), precision: 2);
        Assert.Equal(expected.TotalFats, recipe.GetProperty("totalFats").GetDouble(), precision: 2);
        Assert.Equal(expected.TotalCarbs, recipe.GetProperty("totalCarbs").GetDouble(), precision: 2);
        Assert.Equal(expected.TotalFiber, recipe.GetProperty("totalFiber").GetDouble(), precision: 2);
        Assert.Equal(expected.TotalAlcohol, recipe.GetProperty("totalAlcohol").GetDouble(), precision: 2);

        JsonElement steps = recipe.GetProperty("steps");
        JsonElement step = Assert.Single(steps.EnumerateArray());
        Assert.Equal(expected.StepTitle, step.GetProperty("title").GetString());
        Assert.Equal(expected.StepInstruction, step.GetProperty("instruction").GetString());

        JsonElement ingredient = Assert.Single(step.GetProperty("ingredients").EnumerateArray());
        Assert.Equal(productId, ingredient.GetProperty("productId").GetGuid());
        Assert.Equal(expected.IngredientAmount, ingredient.GetProperty("amount").GetDouble(), precision: 2);
    }

    private static async Task<ProductPayload> CreateIngredientProductAsync(HttpClient client) {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/products",
            new CreateProductHttpRequest(
                Barcode: null,
                "Postgres Recipe Ingredient",
                Brand: "Recipe Test",
                "Grain",
                Category: "Pantry",
                Description: "Ingredient for recipe flow",
                Comment: null,
                ImageUrl: null,
                ImageAssetId: null,
                "G",
                100,
                100,
                200,
                10,
                5,
                30,
                4,
                0,
                "Private")).ConfigureAwait(false);

        await AssertStatusCodeAsync(HttpStatusCode.Created, response).ConfigureAwait(false);
        ProductPayload? payload = await response.Content.ReadFromJsonAsync<ProductPayload>(JsonOptions).ConfigureAwait(false);
        Assert.NotNull(payload);
        return payload;
    }

    private static async Task<string> RegisterAndGetAccessTokenAsync(HttpClient client) {
        string email = $"postgres-recipe-tests-{Guid.NewGuid():N}@example.com";
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
    private sealed record RecipePayload(Guid Id);

    [ExcludeFromCodeCoverage]
    private sealed record ExpectedRecipe(
        string Name,
        string Description,
        string Comment,
        string Category,
        int PrepTime,
        int CookTime,
        int Servings,
        string Visibility,
        string StepTitle,
        string StepInstruction,
        double IngredientAmount,
        double TotalCalories,
        double TotalProteins,
        double TotalFats,
        double TotalCarbs,
        double TotalFiber,
        double TotalAlcohol);
}
