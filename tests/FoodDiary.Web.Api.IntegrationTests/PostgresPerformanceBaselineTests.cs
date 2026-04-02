using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Enums;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Presentation.Api.Features.Auth.Requests;
using FoodDiary.Presentation.Api.Features.Consumptions.Requests;
using FoodDiary.Presentation.Api.Features.Images.Requests;
using FoodDiary.Presentation.Api.Features.Products.Requests;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Web.Api.IntegrationTests;

public sealed class PostgresPerformanceBaselineTests(PostgresApiWebApplicationFactory factory)
    : IClassFixture<PostgresApiWebApplicationFactory> {
    private const int SeedCount = 1500;
    private static readonly TimeSpan RefreshLatencyBudget = TimeSpan.FromMilliseconds(1800);
    private static readonly TimeSpan ProductListLatencyBudget = TimeSpan.FromMilliseconds(400);
    private static readonly TimeSpan RecipeListLatencyBudget = TimeSpan.FromMilliseconds(400);
    private static readonly TimeSpan ConsumptionListLatencyBudget = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan ImageUploadUrlLatencyBudget = TimeSpan.FromMilliseconds(300);

    private static readonly JsonSerializerOptions JsonOptions = new() {
        PropertyNameCaseInsensitive = true,
    };

    [RequiresDockerFact]
    public async Task Refresh_WithWarmTokenRotation_StaysWithinLatencyBudget() {
        var client = factory.CreateClient();
        var email = $"perf-refresh-{Guid.NewGuid():N}@example.com";

        var registerPayload = await RegisterAsync(client, email);
        var firstWarmPayload = await RefreshAsync(client, registerPayload.RefreshToken);
        var warmedPayload = await RefreshAsync(client, firstWarmPayload.RefreshToken);

        var stopwatch = Stopwatch.StartNew();
        var measuredResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new RefreshTokenHttpRequest(warmedPayload.RefreshToken));
        stopwatch.Stop();

        var measuredPayload = await measuredResponse.Content.ReadFromJsonAsync<AuthPayload>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, measuredResponse.StatusCode);
        Assert.NotNull(measuredPayload);
        Assert.False(string.IsNullOrWhiteSpace(measuredPayload.AccessToken));
        Assert.True(
            stopwatch.Elapsed <= RefreshLatencyBudget,
            $"Expected auth.refresh to stay within {RefreshLatencyBudget.TotalMilliseconds} ms, but observed {stopwatch.Elapsed.TotalMilliseconds:F1} ms.");
    }

    [RequiresDockerFact]
    public async Task Products_FirstOwnedPage_StaysWithinEndpointLatencyBudget() {
        var client = factory.CreateClient();
        var email = $"perf-products-{Guid.NewGuid():N}@example.com";
        var authPayload = await RegisterAsync(client, email);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authPayload.AccessToken);

        await SeedProductsAsync(email, SeedCount);

        _ = await client.GetAsync("/api/v1/products?page=1&limit=25&includePublic=false");

        var stopwatch = Stopwatch.StartNew();
        var response = await client.GetAsync("/api/v1/products?page=1&limit=25&includePublic=false");
        stopwatch.Stop();

        await AssertStatusCodeAsync(HttpStatusCode.OK, response);
        var payload = await response.Content.ReadFromJsonAsync<PagedPayload<ItemPayload>>(JsonOptions);

        Assert.NotNull(payload);
        Assert.Equal(SeedCount, payload.TotalItems);
        Assert.Equal(25, payload.Data.Count);
        Assert.True(
            stopwatch.Elapsed <= ProductListLatencyBudget,
            $"Expected GET /api/v1/products first owned page to stay within {ProductListLatencyBudget.TotalMilliseconds} ms, but observed {stopwatch.Elapsed.TotalMilliseconds:F1} ms.");
    }

    [RequiresDockerFact]
    public async Task Recipes_FirstOwnedPage_StaysWithinEndpointLatencyBudget() {
        var client = factory.CreateClient();
        var email = $"perf-recipes-{Guid.NewGuid():N}@example.com";
        var authPayload = await RegisterAsync(client, email);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authPayload.AccessToken);

        await SeedRecipesAsync(email, SeedCount);

        _ = await client.GetAsync("/api/v1/recipes?page=1&limit=25&includePublic=false");

        var stopwatch = Stopwatch.StartNew();
        var response = await client.GetAsync("/api/v1/recipes?page=1&limit=25&includePublic=false");
        stopwatch.Stop();

        await AssertStatusCodeAsync(HttpStatusCode.OK, response);
        var payload = await response.Content.ReadFromJsonAsync<PagedPayload<ItemPayload>>(JsonOptions);

        Assert.NotNull(payload);
        Assert.Equal(SeedCount, payload.TotalItems);
        Assert.Equal(25, payload.Data.Count);
        Assert.True(
            stopwatch.Elapsed <= RecipeListLatencyBudget,
            $"Expected GET /api/v1/recipes first owned page to stay within {RecipeListLatencyBudget.TotalMilliseconds} ms, but observed {stopwatch.Elapsed.TotalMilliseconds:F1} ms.");
    }

    [RequiresDockerFact]
    public async Task Consumptions_FirstPageWithinMonthRange_StaysWithinEndpointLatencyBudget() {
        var client = factory.CreateClient();
        var email = $"perf-consumptions-{Guid.NewGuid():N}@example.com";
        var authPayload = await RegisterAsync(client, email);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authPayload.AccessToken);

        await SeedConsumptionsAsync(client, SeedCount);

        const string url = "/api/v1/consumptions?page=1&limit=25&dateFrom=2026-03-01&dateTo=2026-03-31";

        _ = await client.GetAsync(url);

        var stopwatch = Stopwatch.StartNew();
        var response = await client.GetAsync(url);
        stopwatch.Stop();

        await AssertStatusCodeAsync(HttpStatusCode.OK, response);
        var payload = await response.Content.ReadFromJsonAsync<PagedPayload<ItemPayload>>(JsonOptions);

        Assert.NotNull(payload);
        Assert.Equal(31, payload.TotalItems);
        Assert.Equal(25, payload.Data.Count);
        Assert.True(
            stopwatch.Elapsed <= ConsumptionListLatencyBudget,
            $"Expected GET /api/v1/consumptions first page within monthly range to stay within {ConsumptionListLatencyBudget.TotalMilliseconds} ms, but observed {stopwatch.Elapsed.TotalMilliseconds:F1} ms.");
    }

    [RequiresDockerFact]
    public async Task ImageUploadUrl_WithAuthenticatedUser_StaysWithinLatencyBudget() {
        var client = factory.CreateClient();
        var email = $"perf-images-{Guid.NewGuid():N}@example.com";
        var authPayload = await RegisterAsync(client, email);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authPayload.AccessToken);

        _ = await client.PostAsJsonAsync(
            "/api/v1/images/upload-url",
            new GetImageUploadUrlHttpRequest("warmup-photo.jpg", "image/jpeg", 4096));

        var stopwatch = Stopwatch.StartNew();
        var response = await client.PostAsJsonAsync(
            "/api/v1/images/upload-url",
            new GetImageUploadUrlHttpRequest("measured-photo.jpg", "image/jpeg", 4096));
        stopwatch.Stop();

        var payload = await response.Content.ReadFromJsonAsync<ImageUploadPayload>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.NotEqual(Guid.Empty, payload.AssetId);
        Assert.True(
            stopwatch.Elapsed <= ImageUploadUrlLatencyBudget,
            $"Expected POST /api/v1/images/upload-url to stay within {ImageUploadUrlLatencyBudget.TotalMilliseconds} ms, but observed {stopwatch.Elapsed.TotalMilliseconds:F1} ms.");
    }

    private static async Task<AuthPayload> RegisterAsync(HttpClient client, string email) {
        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterHttpRequest(email, "Password123!", "en"));
        var payload = await response.Content.ReadFromJsonAsync<AuthPayload>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(payload.RefreshToken));
        return payload;
    }

    private static async Task<AuthPayload> RefreshAsync(HttpClient client, string refreshToken) {
        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new RefreshTokenHttpRequest(refreshToken));
        var payload = await response.Content.ReadFromJsonAsync<AuthPayload>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload.RefreshToken));
        return payload;
    }

    private async Task SeedProductsAsync(string email, int count) {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FoodDiaryDbContext>();
        var user = await dbContext.Users.SingleAsync(x => x.Email == email);

        var products = Enumerable.Range(0, count)
            .Select(index => Product.Create(
                user.Id,
                $"Perf Product {index:D4}",
                MeasurementUnit.G,
                100,
                25,
                100,
                10,
                5,
                20,
                3,
                0,
                visibility: Visibility.Private))
            .ToArray();

        dbContext.Products.AddRange(products);
        await dbContext.SaveChangesAsync();
    }

    private async Task SeedRecipesAsync(string email, int count) {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FoodDiaryDbContext>();
        var user = await dbContext.Users.SingleAsync(x => x.Email == email);

        var recipes = Enumerable.Range(0, count)
            .Select(index => Recipe.Create(
                user.Id,
                $"Perf Recipe {index:D4}",
                servings: 2,
                description: $"Description {index:D4}",
                visibility: Visibility.Private))
            .ToArray();

        dbContext.Recipes.AddRange(recipes);
        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedConsumptionsAsync(HttpClient client, int count) {
        var createProductResponse = await client.PostAsJsonAsync(
            "/api/v1/products",
            new CreateProductHttpRequest(
                null,
                "Perf Consumption Product",
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
        await AssertStatusCodeAsync(HttpStatusCode.Created, createProductResponse);
        var product = await createProductResponse.Content.ReadFromJsonAsync<ItemPayload>(JsonOptions);
        Assert.NotNull(product);

        var startDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var mealTypes = new[] { MealType.Breakfast, MealType.Lunch, MealType.Dinner, MealType.Snack };

        foreach (var index in Enumerable.Range(0, count)) {
            var createConsumptionResponse = await client.PostAsJsonAsync(
                "/api/v1/consumptions",
                new CreateConsumptionHttpRequest(
                    startDate.AddDays(index),
                    mealTypes[index % mealTypes.Length].ToString(),
                    $"Perf Meal {index:D4}",
                    null,
                    null,
                    [new ConsumptionItemHttpRequest(product.Id, null, 100)],
                    IsNutritionAutoCalculated: true));
            await AssertStatusCodeAsync(HttpStatusCode.Created, createConsumptionResponse);
        }
    }

    private static async Task AssertStatusCodeAsync(HttpStatusCode expected, HttpResponseMessage response) {
        if (response.StatusCode == expected) {
            return;
        }

        var body = await response.Content.ReadAsStringAsync();
        Assert.Fail(
            $"Expected status {(int)expected} ({expected}), got {(int)response.StatusCode} ({response.StatusCode}). Body: {body}");
    }

    private sealed record AuthPayload(string AccessToken, string RefreshToken, AuthUserPayload User);

    private sealed record AuthUserPayload(string Email);

    private sealed record PagedPayload<T>(IReadOnlyList<T> Data, int Page, int Limit, int TotalPages, int TotalItems);

    private sealed record ItemPayload(Guid Id);

    private sealed record ImageUploadPayload(string UploadUrl, string FileUrl, string ObjectKey, DateTime ExpiresAtUtc, Guid AssetId);
}
