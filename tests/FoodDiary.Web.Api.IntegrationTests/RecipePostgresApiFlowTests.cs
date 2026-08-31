using FoodDiary.Application.Abstractions.Products.Common;
using FoodDiary.Application.Abstractions.Recipes.Common;
using FoodDiary.Application.Recipes.Common;
using FoodDiary.Application.Recipes.Services;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
    public async Task NestedRecipe_CycleAccessDuplicateAndDeletion_PreservePostgresBoundaries() {
        using HttpClient owner = factory.CreateClient();
        owner.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", await RegisterAndGetAccessTokenAsync(owner));
        using HttpClient stranger = factory.CreateClient();
        stranger.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", await RegisterAndGetAccessTokenAsync(stranger));
        ProductPayload product = await CreateIngredientProductAsync(owner);
        var expected = new ExpectedRecipe("Nested base", "Description", "Private note", "Dinner",
            5, 10, 4, "Private", "Cook", "Cook ingredient", 100, 400, 20, 10, 60, 8, 0);
        CreateRecipeHttpRequest baseRequest = CreateRecipeRequest(expected, product.Id) with {
            CalculateNutritionAutomatically = false,
            ManualCalories = 400,
            ManualProteins = 20,
            ManualFats = 10,
            ManualCarbs = 60,
            ManualFiber = 8,
            ManualAlcohol = 0,
        };
        HttpResponseMessage baseResponse = await owner.PostAsJsonAsync("/api/v1/recipes", baseRequest);
        await AssertStatusCodeAsync(HttpStatusCode.Created, baseResponse);
        RecipePayload child = (await baseResponse.Content.ReadFromJsonAsync<RecipePayload>(JsonOptions))!;
        Assert.NotNull(child);
        RecipeStepHttpRequest nestedStep = new("Mix", "Mix nested recipe",
            [new RecipeIngredientHttpRequest(ProductId: null, child.Id, Amount: 2)], ImageUrl: null, ImageAssetId: null);
        CreateRecipeHttpRequest parentRequest = CreateRecipeRequest(expected, product.Id) with {
            Name = "Nested parent",
            Servings = 2,
            Steps = [nestedStep],
        };
        HttpResponseMessage parentResponse = await owner.PostAsJsonAsync("/api/v1/recipes", parentRequest);
        await AssertStatusCodeAsync(HttpStatusCode.Created, parentResponse);
        RecipePayload parent = (await parentResponse.Content.ReadFromJsonAsync<RecipePayload>(JsonOptions))!;
        Assert.NotNull(parent);
        using var parentJson = JsonDocument.Parse(await owner.GetStringAsync($"/api/v1/recipes/{parent.Id}"));
        Assert.Equal(200, parentJson.RootElement.GetProperty("totalCalories").GetDouble());

        await AssertStatusCodeAsync(HttpStatusCode.NotFound, await stranger.GetAsync($"/api/v1/recipes/{parent.Id}"));
        await AssertStatusCodeAsync(HttpStatusCode.NotFound, await stranger.DeleteAsync($"/api/v1/recipes/{parent.Id}"));
        await AssertStatusCodeAsync(HttpStatusCode.NotFound,
            await stranger.PostAsJsonAsync($"/api/v1/recipes/{parent.Id}/duplicate", new { }));

        UpdateRecipeHttpRequest cycleRequest = UpdateRecipeRequest(expected, product.Id) with {
            Name = "Must not persist",
            Steps = [new RecipeStepHttpRequest("Cycle", "Invalid cycle",
                [new RecipeIngredientHttpRequest(ProductId: null, parent.Id, Amount: 1)], ImageUrl: null, ImageAssetId: null)],
        };
        HttpResponseMessage cycleResponse = await owner.PatchAsJsonAsync($"/api/v1/recipes/{child.Id}", cycleRequest);
        await AssertStatusCodeAsync(HttpStatusCode.BadRequest, cycleResponse);
        Assert.Contains("already used", await cycleResponse.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        // HTTP rejects mutation of a used recipe first; exercise cycle validation against the real lookup adapters too.
        await using (AsyncServiceScope scope = factory.Services.CreateAsyncScope()) {
            FoodDiaryDbContext context = scope.ServiceProvider.GetRequiredService<FoodDiaryDbContext>();
            var childId = new RecipeId(child.Id);
            Recipe storedChild = await context.Recipes.SingleAsync(value => value.Id == childId);
            Result cycleResult = await RecipeIngredientAccessValidator.EnsureIngredientsAccessibleAsync(
                [new RecipeStepInput(1, "Invalid cycle", Title: null, ImageUrl: null, ImageAssetId: null,
                    [new RecipeIngredientInput(ProductId: null, parent.Id, Amount: 1)])],
                childId,
                storedChild.UserId,
                scope.ServiceProvider.GetRequiredService<IProductLookupService>(),
                scope.ServiceProvider.GetRequiredService<IRecipeLookupService>(),
                CancellationToken.None);
            Assert.True(cycleResult.IsFailure);
            Assert.Contains("circular dependency", cycleResult.Error.Message, StringComparison.Ordinal);
        }
        await AssertRecipeDetailAsync(owner, child.Id, product.Id, expected);
        await AssertStatusCodeAsync(HttpStatusCode.BadRequest, await owner.DeleteAsync($"/api/v1/recipes/{child.Id}"));

        HttpResponseMessage duplicateResponse = await owner.PostAsJsonAsync($"/api/v1/recipes/{parent.Id}/duplicate", new { });
        await AssertStatusCodeAsync(HttpStatusCode.OK, duplicateResponse);
        RecipePayload duplicate = (await duplicateResponse.Content.ReadFromJsonAsync<RecipePayload>(JsonOptions))!;
        Assert.NotNull(duplicate);
        Assert.NotEqual(parent.Id, duplicate.Id);
        using var duplicateJson = JsonDocument.Parse(await owner.GetStringAsync($"/api/v1/recipes/{duplicate.Id}"));
        Assert.Equal(200, duplicateJson.RootElement.GetProperty("totalCalories").GetDouble());
        Assert.NotEqual(
            parentJson.RootElement.GetProperty("steps")[0].GetProperty("id").GetGuid(),
            duplicateJson.RootElement.GetProperty("steps")[0].GetProperty("id").GetGuid());
        Assert.Equal(child.Id, duplicateJson.RootElement.GetProperty("steps")[0]
            .GetProperty("ingredients")[0].GetProperty("nestedRecipeId").GetGuid());
        await AssertStatusCodeAsync(HttpStatusCode.NoContent, await owner.DeleteAsync($"/api/v1/recipes/{parent.Id}"));
        await AssertStatusCodeAsync(HttpStatusCode.BadRequest, await owner.DeleteAsync($"/api/v1/recipes/{child.Id}"));
        await AssertStatusCodeAsync(HttpStatusCode.NoContent, await owner.DeleteAsync($"/api/v1/recipes/{duplicate.Id}"));
        await AssertStatusCodeAsync(HttpStatusCode.NoContent, await owner.DeleteAsync($"/api/v1/recipes/{child.Id}"));
        await AssertStatusCodeAsync(HttpStatusCode.NotFound, await owner.GetAsync($"/api/v1/recipes/{child.Id}"));
    }

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
