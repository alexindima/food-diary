using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FoodDiary.Presentation.Api.Features.Auth.Requests;
using FoodDiary.Presentation.Api.Features.Consumptions.Requests;
using FoodDiary.Presentation.Api.Features.FavoriteProducts.Requests;
using FoodDiary.Presentation.Api.Features.Products.Requests;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;
using Xunit.Abstractions;

namespace FoodDiary.Web.Api.IntegrationTests;

public sealed class AuthAndProductsFlowTests(ApiWebApplicationFactory factory, ITestOutputHelper output)
    : IClassFixture<ApiWebApplicationFactory> {
    private static readonly JsonSerializerOptions JsonOptions = new() {
        PropertyNameCaseInsensitive = true,
    };

    [Fact]
    public async Task Register_ReturnsAuthenticationTokens() {
        var client = factory.CreateClient();
        var email = $"api-tests-{Guid.NewGuid():N}@example.com";
        var request = new RegisterHttpRequest(email, "Password123!", "en");

        var response = await client.PostAsJsonAsync("/api/v1/auth/register", request);
        var body = await response.Content.ReadAsStringAsync();
        output.WriteLine(body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = JsonSerializer.Deserialize<AuthPayload>(body, JsonOptions);
        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(payload.RefreshToken));
        Assert.Equal(email, payload.User.Email);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized() {
        var client = factory.CreateClient();
        var email = $"api-tests-{Guid.NewGuid():N}@example.com";

        var registerResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterHttpRequest(email, "Password123!", "en"));
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        var loginResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginHttpRequest(email, "WrongPassword123!"));

        Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode);
    }

    [Fact]
    public async Task Products_RequiresAuth_AndReturnsOkWithBearerToken() {
        var client = factory.CreateClient();
        var anonymousResponse = await client.GetAsync("/api/v1/products");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);

        var email = $"api-tests-{Guid.NewGuid():N}@example.com";
        var registerResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterHttpRequest(email, "Password123!", "en"));
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        var authPayload = await registerResponse.Content.ReadFromJsonAsync<AuthPayload>(JsonOptions);
        Assert.NotNull(authPayload);
        Assert.False(string.IsNullOrWhiteSpace(authPayload.AccessToken));

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authPayload.AccessToken);
        var authorizedResponse = await client.GetAsync("/api/v1/products");

        Assert.Equal(HttpStatusCode.OK, authorizedResponse.StatusCode);
    }

    [Fact]
    public async Task UsersInfo_RequiresAuth_AndReturnsOkWithBearerToken() {
        var client = factory.CreateClient();
        var anonymousResponse = await client.GetAsync("/api/v1/users/info");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);

        var email = $"api-tests-{Guid.NewGuid():N}@example.com";
        var registerResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterHttpRequest(email, "Password123!", "en"));
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        var authPayload = await registerResponse.Content.ReadFromJsonAsync<AuthPayload>(JsonOptions);
        Assert.NotNull(authPayload);
        Assert.False(string.IsNullOrWhiteSpace(authPayload.AccessToken));

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authPayload.AccessToken);
        var authorizedResponse = await client.GetAsync("/api/v1/users/info");

        Assert.NotEqual(HttpStatusCode.Unauthorized, authorizedResponse.StatusCode);
    }

    [Fact]
    public async Task CreateProduct_ReturnsCreatedAndLocationHeader() {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync(
            "/api/v1/products",
            new CreateProductHttpRequest(
                null,
                "Created Product",
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

        var payload = await response.Content.ReadFromJsonAsync<ProductPayload>(JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(payload);
        Assert.NotNull(response.Headers.Location);
        Assert.EndsWith($"/api/v1/Products/{payload.Id}", response.Headers.Location.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProductsOverview_AndRecent_ReturnFavoritePreviewRecentItemsAndFavoriteFlags() {
        var client = await CreateAuthenticatedClientAsync();
        var firstProductId = await CreateProductAsync(client, "Overview Apple");
        var favoriteProductId = await CreateProductAsync(client, "Overview Chicken");

        var favoriteResponse = await client.PostAsJsonAsync(
            "/api/v1/favorite-products",
            new AddFavoriteProductHttpRequest(favoriteProductId, "Favorite chicken"));
        favoriteResponse.EnsureSuccessStatusCode();

        var consumptionResponse = await client.PostAsJsonAsync(
            "/api/v1/consumptions",
            new CreateConsumptionHttpRequest(
                DateTime.UtcNow.Date,
                "Lunch",
                null,
                null,
                null,
                [new ConsumptionItemHttpRequest(favoriteProductId, null, 200)]));
        consumptionResponse.EnsureSuccessStatusCode();

        var overviewResponse = await client.GetAsync("/api/v1/products/overview?page=1&limit=10&includePublic=true&recentLimit=10&favoriteLimit=10");
        var recentResponse = await client.GetAsync("/api/v1/products/recent?limit=10&includePublic=true");

        overviewResponse.EnsureSuccessStatusCode();
        recentResponse.EnsureSuccessStatusCode();

        using var overviewJson = JsonDocument.Parse(await overviewResponse.Content.ReadAsStringAsync());
        using var recentJson = JsonDocument.Parse(await recentResponse.Content.ReadAsStringAsync());

        var overviewRoot = overviewJson.RootElement;
        var recentItems = recentJson.RootElement;
        var favoriteItems = overviewRoot.GetProperty("favoriteItems");
        var recentOverviewItems = overviewRoot.GetProperty("recentItems");
        var allProducts = overviewRoot.GetProperty("allProducts").GetProperty("data");

        Assert.Equal(1, overviewRoot.GetProperty("favoriteTotalCount").GetInt32());
        Assert.Contains(favoriteItems.EnumerateArray(), item => item.GetProperty("productId").GetGuid() == favoriteProductId);
        Assert.Contains(recentOverviewItems.EnumerateArray(), item => item.GetProperty("id").GetGuid() == favoriteProductId);
        Assert.Contains(recentItems.EnumerateArray(), item => item.GetProperty("id").GetGuid() == favoriteProductId);

        var favoriteProduct = allProducts.EnumerateArray().Single(item => item.GetProperty("id").GetGuid() == favoriteProductId);
        var nonFavoriteProduct = allProducts.EnumerateArray().Single(item => item.GetProperty("id").GetGuid() == firstProductId);
        Assert.True(favoriteProduct.GetProperty("isFavorite").GetBoolean());
        Assert.NotEqual(Guid.Empty, favoriteProduct.GetProperty("favoriteProductId").GetGuid());
        Assert.False(nonFavoriteProduct.GetProperty("isFavorite").GetBoolean());
    }

    [Fact]
    public async Task UpdateProduct_PersistsPatchedValues() {
        var client = await CreateAuthenticatedClientAsync();
        var productId = await CreateProductAsync(client, "Patchable Product");

        var updateResponse = await client.PatchAsJsonAsync(
            $"/api/v1/products/{productId}",
            new UpdateProductHttpRequest(
                Barcode: null,
                ClearBarcode: false,
                Name: "Updated Product",
                Brand: "Updated Brand",
                ClearBrand: false,
                ProductType: "Food",
                Category: "Snacks",
                ClearCategory: false,
                Description: "Updated description",
                ClearDescription: false,
                Comment: "Updated comment",
                ClearComment: false,
                ImageUrl: null,
                ClearImageUrl: false,
                ImageAssetId: null,
                ClearImageAssetId: false,
                BaseUnit: "G",
                BaseAmount: 100,
                DefaultPortionAmount: 140,
                CaloriesPerBase: 150,
                ProteinsPerBase: 8,
                FatsPerBase: 5,
                CarbsPerBase: 16,
                FiberPerBase: 2,
                AlcoholPerBase: 0,
                Visibility: "Private"));
        updateResponse.EnsureSuccessStatusCode();

        var getResponse = await client.GetAsync($"/api/v1/products/{productId}");
        getResponse.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await getResponse.Content.ReadAsStringAsync());

        Assert.Equal("Updated Product", json.RootElement.GetProperty("name").GetString());
        Assert.Equal("Updated Brand", json.RootElement.GetProperty("brand").GetString());
        Assert.Equal("Updated comment", json.RootElement.GetProperty("comment").GetString());
        Assert.Equal(140, json.RootElement.GetProperty("defaultPortionAmount").GetDouble());
    }

    [Fact]
    public async Task DuplicateProduct_ReturnsIndependentCopy() {
        var client = await CreateAuthenticatedClientAsync();
        var originalId = await CreateProductAsync(client, "Original Product");

        var duplicateResponse = await client.PostAsJsonAsync($"/api/v1/products/{originalId}/duplicate", new { });
        duplicateResponse.EnsureSuccessStatusCode();
        var duplicate = await duplicateResponse.Content.ReadFromJsonAsync<ProductPayload>(JsonOptions);

        Assert.NotNull(duplicate);
        Assert.NotEqual(originalId, duplicate.Id);

        var duplicateGetResponse = await client.GetAsync($"/api/v1/products/{duplicate.Id}");
        duplicateGetResponse.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await duplicateGetResponse.Content.ReadAsStringAsync());

        Assert.Equal("Original Product", json.RootElement.GetProperty("name").GetString());
    }

    [Fact]
    public async Task DeleteProduct_RemovesItFromSubsequentRead() {
        var client = await CreateAuthenticatedClientAsync();
        var productId = await CreateProductAsync(client, "Delete Me");

        var deleteResponse = await client.DeleteAsync($"/api/v1/products/{productId}");
        var getResponse = await client.GetAsync($"/api/v1/products/{productId}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync() {
        var client = factory.CreateClient();
        var email = $"api-tests-{Guid.NewGuid():N}@example.com";
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

        var payload = await response.Content.ReadFromJsonAsync<ProductPayload>(JsonOptions);
        Assert.NotNull(payload);
        return payload.Id;
    }

    private sealed record AuthPayload(string AccessToken, string RefreshToken, AuthUserPayload User);

    private sealed record AuthUserPayload(string Email);

    private sealed record ProductPayload(Guid Id);
}
