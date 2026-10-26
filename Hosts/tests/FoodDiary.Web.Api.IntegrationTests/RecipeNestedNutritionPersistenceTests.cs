using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Identity.Presentation.Features.Auth.Requests;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class RecipeNestedNutritionPersistenceTests(ApiWebApplicationFactory factory)
    : IClassFixture<ApiWebApplicationFactory> {
    [RequiresDockerFact]
    public async Task AddingNestedServings_PatchStoredColumnsAndGetAgreeAsync() {
        using HttpClient client = factory.CreateClient();
        HttpResponseMessage registration = await client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterHttpRequest($"nested-nutrition-{Guid.NewGuid():N}@example.test", "Password123!", "en"));
        registration.EnsureSuccessStatusCode();
        using var auth = JsonDocument.Parse(await registration.Content.ReadAsStringAsync());
        AuthenticationResponseCookies.ApplyBrowserSession(client, AuthenticationResponseCookies.ReadBrowserSession(registration));
        Guid productId = await CreateAsync(client, "/api/v1/products", new {
            name = "Base product",
            productType = "Other",
            baseUnit = "G",
            baseAmount = 100,
            defaultPortionAmount = 100,
            caloriesPerBase = 100,
            proteinsPerBase = 10,
            fatsPerBase = 0,
            carbsPerBase = 15,
            fiberPerBase = 0,
            alcoholPerBase = 0,
            visibility = "Private",
        });
        Guid nestedId = await CreateAsync(client, "/api/v1/recipes", new {
            name = "Nested recipe",
            servings = 2,
            visibility = "Private",
            language = "en",
            languageConfirmed = true,
            calculateNutritionAutomatically = false,
            manualCalories = 200,
            manualProteins = 20,
            manualFats = 0,
            manualCarbs = 30,
            manualFiber = 0,
            manualAlcohol = 0,
            steps = new[] { new { description = "Prepare", ingredients = Array.Empty<object>() } },
        });
        Guid parentId = await CreateAsync(client, "/api/v1/recipes", new {
            name = "Parent recipe",
            servings = 1,
            visibility = "Private",
            language = "en",
            languageConfirmed = true,
            calculateNutritionAutomatically = true,
            steps = new[] { new { description = "Mix", ingredients = new[] { new { productId, amount = 200 } } } },
        });

        foreach ((double amount, double calories, double proteins, double carbs) in new[] {
            (1d, 300d, 30d, 45d), (0.5d, 250d, 25d, 37.5d), (0.75d, 275d, 27.5d, 41.25d),
        }) {
            HttpResponseMessage patch = await client.PatchAsJsonAsync($"/api/v1/recipes/{parentId}", new {
                calculateNutritionAutomatically = true,
                steps = new[] { new { description = "Mix", ingredients = new object[] {
                    new { productId, amount = 200 }, new { nestedRecipeId = nestedId, amount },
                    new { textName = "Herbs", amountText = "a pinch", amount = 0 },
                }, }, },
            });
            if (!patch.IsSuccessStatusCode) {
                Assert.Fail(await patch.Content.ReadAsStringAsync());
            }
            Assert.Equal(HttpStatusCode.OK, patch.StatusCode);
            using var patched = JsonDocument.Parse(await patch.Content.ReadAsStringAsync());
            AssertNutrition(patched.RootElement, calories, proteins, carbs);
            await AssertStoredNutritionAsync(parentId, calories, proteins, carbs);
            HttpResponseMessage get = await client.GetAsync($"/api/v1/recipes/{parentId}");
            Assert.Equal(HttpStatusCode.OK, get.StatusCode);
            using var read = JsonDocument.Parse(await get.Content.ReadAsStringAsync());
            AssertNutrition(read.RootElement, calories, proteins, carbs);
            await AssertStoredNutritionAsync(parentId, calories, proteins, carbs);
        }
    }

    private static async Task<Guid> CreateAsync(HttpClient client, string path, object input) {
        HttpResponseMessage response = await client.PostAsJsonAsync(path, input);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("id").GetGuid();
    }

    private async Task AssertStoredNutritionAsync(Guid id, double calories, double proteins, double carbs) {
        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();
        FoodDiaryDbContext database = scope.ServiceProvider.GetRequiredService<FoodDiaryDbContext>();
        await using var connection = new NpgsqlConnection(database.Database.GetConnectionString());
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("""
            SELECT "TotalCalories", "TotalProteins", "TotalCarbs" FROM "Recipes" WHERE "Id" = @id
            """, connection);
        command.Parameters.AddWithValue("id", id);
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal(calories, reader.GetDouble(0));
        Assert.Equal(proteins, reader.GetDouble(1));
        Assert.Equal(carbs, reader.GetDouble(2));
    }

    private static void AssertNutrition(JsonElement recipe, double calories, double proteins, double carbs) {
        Assert.Equal(calories, recipe.GetProperty("totalCalories").GetDouble());
        Assert.Equal(proteins, recipe.GetProperty("totalProteins").GetDouble());
        Assert.Equal(carbs, recipe.GetProperty("totalCarbs").GetDouble());
    }
}
