using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FoodDiary.Presentation.Api.Features.Auth.Requests;
using FoodDiary.Presentation.Api.Features.Products.Requests;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class ProductPostgresApiFlowTests(PostgresApiWebApplicationFactory factory)
    : IClassFixture<PostgresApiWebApplicationFactory> {
    private static readonly JsonSerializerOptions JsonOptions = new() {
        PropertyNameCaseInsensitive = true,
    };

    [RequiresDockerTheory]
    [MemberData(nameof(ProductCreateUpdateReadCases))]
    public async Task CreateUpdateReadProduct_PersistsRepresentativePayloadsAgainstPostgres(ProductCreateUpdateReadCase testCase) {
        HttpClient client = factory.CreateClient();
        string accessToken = await RegisterAndGetAccessTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        HttpResponseMessage createResponse = await client.PostAsJsonAsync("/api/v1/products", testCase.CreateRequest);
        ProductPayload? product = await createResponse.Content.ReadFromJsonAsync<ProductPayload>(JsonOptions);

        await AssertStatusCodeAsync(HttpStatusCode.Created, createResponse);
        Assert.NotNull(product);

        HttpResponseMessage createdReadResponse = await client.GetAsync($"/api/v1/products/{product.Id}");
        await AssertStatusCodeAsync(HttpStatusCode.OK, createdReadResponse);
        using var createdJson = JsonDocument.Parse(await createdReadResponse.Content.ReadAsStringAsync());
        AssertProductJson(testCase.ExpectedCreated, createdJson.RootElement);

        HttpResponseMessage updateResponse = await client.PatchAsJsonAsync($"/api/v1/products/{product.Id}", testCase.UpdateRequest);
        await AssertStatusCodeAsync(HttpStatusCode.OK, updateResponse);

        HttpResponseMessage updatedReadResponse = await client.GetAsync($"/api/v1/products/{product.Id}");
        await AssertStatusCodeAsync(HttpStatusCode.OK, updatedReadResponse);
        using var updatedJson = JsonDocument.Parse(await updatedReadResponse.Content.ReadAsStringAsync());
        AssertProductJson(testCase.ExpectedUpdated, updatedJson.RootElement);
    }

    public static IEnumerable<object[]> ProductCreateUpdateReadCases() {
        yield return [
            new ProductCreateUpdateReadCase(
                Name: "grams with optional fields",
                CreateRequest: new CreateProductHttpRequest(
                    Barcode: "4600000000001",
                    "Postgres Product Grams",
                    Brand: "Test Brand",
                    "Grain",
                    Category: "Pantry",
                    Description: "Create description",
                    Comment: "Create comment",
                    ImageUrl: null,
                    ImageAssetId: null,
                    "G",
                    100,
                    125,
                    245.5,
                    9.2,
                    3.4,
                    42.6,
                    6.1,
                    0,
                    "Private"),
                UpdateRequest: new UpdateProductHttpRequest(
                    Barcode: "4600000000002",
                    ClearBarcode: false,
                    Name: "Postgres Product Grams Updated",
                    Brand: "Updated Brand",
                    ClearBrand: false,
                    ProductType: "Other",
                    Category: "Updated Pantry",
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
                    DefaultPortionAmount: 150,
                    CaloriesPerBase: 260.5,
                    ProteinsPerBase: 10.5,
                    FatsPerBase: 4.5,
                    CarbsPerBase: 43.5,
                    FiberPerBase: 7.5,
                    AlcoholPerBase: 0,
                    Visibility: "Private"),
                ExpectedCreated: new ExpectedProduct(
                    Name: "Postgres Product Grams",
                    Barcode: "4600000000001",
                    Brand: "Test Brand",
                    ProductType: "Grain",
                    Category: "Pantry",
                    Description: "Create description",
                    Comment: "Create comment",
                    ImageUrl: null,
                    BaseUnit: "G",
                    BaseAmount: 100,
                    DefaultPortionAmount: 125,
                    CaloriesPerBase: 245.5,
                    ProteinsPerBase: 9.2,
                    FatsPerBase: 3.4,
                    CarbsPerBase: 42.6,
                    FiberPerBase: 6.1,
                    AlcoholPerBase: 0,
                    Visibility: "Private"),
                ExpectedUpdated: new ExpectedProduct(
                    Name: "Postgres Product Grams Updated",
                    Barcode: "4600000000002",
                    Brand: "Updated Brand",
                    ProductType: "Other",
                    Category: "Updated Pantry",
                    Description: "Updated description",
                    Comment: "Updated comment",
                    ImageUrl: null,
                    BaseUnit: "G",
                    BaseAmount: 100,
                    DefaultPortionAmount: 150,
                    CaloriesPerBase: 260.5,
                    ProteinsPerBase: 10.5,
                    FatsPerBase: 4.5,
                    CarbsPerBase: 43.5,
                    FiberPerBase: 7.5,
                    AlcoholPerBase: 0,
                    Visibility: "Private"))];

        yield return [
            new ProductCreateUpdateReadCase(
                Name: "milliliters public product",
                CreateRequest: new CreateProductHttpRequest(
                    Barcode: null,
                    "Postgres Product Drink",
                    Brand: "Hydration Co",
                    "Beverage",
                    Category: "Drinks",
                    Description: "Public drink",
                    Comment: null,
                    ImageUrl: null,
                    ImageAssetId: null,
                    "ML",
                    100,
                    330,
                    42,
                    0,
                    0,
                    10.2,
                    0,
                    0,
                    "Public"),
                UpdateRequest: new UpdateProductHttpRequest(
                    Barcode: null,
                    ClearBarcode: false,
                    Name: "Postgres Product Drink Updated",
                    Brand: null,
                    ClearBrand: false,
                    ProductType: "Beverage",
                    Category: "Cold drinks",
                    ClearCategory: false,
                    Description: "Updated public drink",
                    ClearDescription: false,
                    Comment: "Now with comment",
                    ClearComment: false,
                    ImageUrl: null,
                    ClearImageUrl: false,
                    ImageAssetId: null,
                    ClearImageAssetId: false,
                    BaseUnit: "Ml",
                    BaseAmount: 100,
                    DefaultPortionAmount: 250,
                    CaloriesPerBase: 38,
                    ProteinsPerBase: 0,
                    FatsPerBase: 0,
                    CarbsPerBase: 9.5,
                    FiberPerBase: 0,
                    AlcoholPerBase: 0,
                    Visibility: "Public"),
                ExpectedCreated: new ExpectedProduct(
                    Name: "Postgres Product Drink",
                    Barcode: null,
                    Brand: "Hydration Co",
                    ProductType: "Beverage",
                    Category: "Drinks",
                    Description: "Public drink",
                    Comment: null,
                    ImageUrl: null,
                    BaseUnit: "Ml",
                    BaseAmount: 100,
                    DefaultPortionAmount: 330,
                    CaloriesPerBase: 42,
                    ProteinsPerBase: 0,
                    FatsPerBase: 0,
                    CarbsPerBase: 10.2,
                    FiberPerBase: 0,
                    AlcoholPerBase: 0,
                    Visibility: "Public"),
                ExpectedUpdated: new ExpectedProduct(
                    Name: "Postgres Product Drink Updated",
                    Barcode: null,
                    Brand: "Hydration Co",
                    ProductType: "Beverage",
                    Category: "Cold drinks",
                    Description: "Updated public drink",
                    Comment: "Now with comment",
                    ImageUrl: null,
                    BaseUnit: "Ml",
                    BaseAmount: 100,
                    DefaultPortionAmount: 250,
                    CaloriesPerBase: 38,
                    ProteinsPerBase: 0,
                    FatsPerBase: 0,
                    CarbsPerBase: 9.5,
                    FiberPerBase: 0,
                    AlcoholPerBase: 0,
                    Visibility: "Public"))];

        yield return [
            new ProductCreateUpdateReadCase(
                Name: "pieces and clear optional fields",
                CreateRequest: new CreateProductHttpRequest(
                    Barcode: "piece-barcode",
                    "Postgres Product Piece",
                    Brand: "Piece Brand",
                    "Other",
                    Category: "Pieces",
                    Description: "One item",
                    Comment: "Keep count",
                    ImageUrl: null,
                    ImageAssetId: null,
                    "PCS",
                    1,
                    2,
                    88,
                    3,
                    2,
                    12,
                    1,
                    0,
                    "Private"),
                UpdateRequest: new UpdateProductHttpRequest(
                    Barcode: null,
                    ClearBarcode: true,
                    Name: "Postgres Product Piece Updated",
                    Brand: null,
                    ClearBrand: true,
                    ProductType: "Other",
                    Category: null,
                    ClearCategory: true,
                    Description: null,
                    ClearDescription: true,
                    Comment: null,
                    ClearComment: true,
                    ImageUrl: null,
                    ClearImageUrl: true,
                    ImageAssetId: null,
                    ClearImageAssetId: true,
                    BaseUnit: "PCS",
                    BaseAmount: 1,
                    DefaultPortionAmount: 3,
                    CaloriesPerBase: 91,
                    ProteinsPerBase: 3.5,
                    FatsPerBase: 2.1,
                    CarbsPerBase: 12.4,
                    FiberPerBase: 1.2,
                    AlcoholPerBase: 0,
                    Visibility: "Private"),
                ExpectedCreated: new ExpectedProduct(
                    Name: "Postgres Product Piece",
                    Barcode: "piece-barcode",
                    Brand: "Piece Brand",
                    ProductType: "Other",
                    Category: "Pieces",
                    Description: "One item",
                    Comment: "Keep count",
                    ImageUrl: null,
                    BaseUnit: "Pcs",
                    BaseAmount: 1,
                    DefaultPortionAmount: 2,
                    CaloriesPerBase: 88,
                    ProteinsPerBase: 3,
                    FatsPerBase: 2,
                    CarbsPerBase: 12,
                    FiberPerBase: 1,
                    AlcoholPerBase: 0,
                    Visibility: "Private"),
                ExpectedUpdated: new ExpectedProduct(
                    Name: "Postgres Product Piece Updated",
                    Barcode: null,
                    Brand: null,
                    ProductType: "Other",
                    Category: null,
                    Description: null,
                    Comment: null,
                    ImageUrl: null,
                    BaseUnit: "Pcs",
                    BaseAmount: 1,
                    DefaultPortionAmount: 3,
                    CaloriesPerBase: 91,
                    ProteinsPerBase: 3.5,
                    FatsPerBase: 2.1,
                    CarbsPerBase: 12.4,
                    FiberPerBase: 1.2,
                    AlcoholPerBase: 0,
                    Visibility: "Private"))];
    }

    private static async Task<string> RegisterAndGetAccessTokenAsync(HttpClient client) {
        string email = $"postgres-product-tests-{Guid.NewGuid():N}@example.com";
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

    private static void AssertProductJson(ExpectedProduct expected, JsonElement product) {
        Assert.Equal(expected.Name, product.GetProperty("name").GetString());
        AssertNullableString(expected.Barcode, product, "barcode");
        AssertNullableString(expected.Brand, product, "brand");
        Assert.Equal(expected.ProductType, product.GetProperty("productType").GetString());
        AssertNullableString(expected.Category, product, "category");
        AssertNullableString(expected.Description, product, "description");
        AssertNullableString(expected.Comment, product, "comment");
        AssertNullableString(expected.ImageUrl, product, "imageUrl");
        Assert.Equal(expected.BaseUnit, product.GetProperty("baseUnit").GetString());
        Assert.Equal(expected.BaseAmount, product.GetProperty("baseAmount").GetDouble());
        Assert.Equal(expected.DefaultPortionAmount, product.GetProperty("defaultPortionAmount").GetDouble());
        Assert.Equal(expected.CaloriesPerBase, product.GetProperty("caloriesPerBase").GetDouble());
        Assert.Equal(expected.ProteinsPerBase, product.GetProperty("proteinsPerBase").GetDouble());
        Assert.Equal(expected.FatsPerBase, product.GetProperty("fatsPerBase").GetDouble());
        Assert.Equal(expected.CarbsPerBase, product.GetProperty("carbsPerBase").GetDouble());
        Assert.Equal(expected.FiberPerBase, product.GetProperty("fiberPerBase").GetDouble());
        Assert.Equal(expected.AlcoholPerBase, product.GetProperty("alcoholPerBase").GetDouble());
        Assert.Equal(expected.Visibility, product.GetProperty("visibility").GetString());
    }

    private static void AssertNullableString(string? expected, JsonElement product, string propertyName) {
        JsonElement property = product.GetProperty(propertyName);
        if (expected is null) {
            Assert.Equal(JsonValueKind.Null, property.ValueKind);
            return;
        }

        Assert.Equal(expected, property.GetString());
    }

    [ExcludeFromCodeCoverage]
    private sealed record AuthPayload(string AccessToken, string RefreshToken, AuthUserPayload User);

    [ExcludeFromCodeCoverage]
    private sealed record AuthUserPayload(string Email);

    [ExcludeFromCodeCoverage]
    private sealed record ProductPayload(Guid Id);

    [ExcludeFromCodeCoverage]
    public sealed record ProductCreateUpdateReadCase(
        string Name,
        CreateProductHttpRequest CreateRequest,
        UpdateProductHttpRequest UpdateRequest,
        ExpectedProduct ExpectedCreated,
        ExpectedProduct ExpectedUpdated) {
        public override string ToString() => Name;
    }

    [ExcludeFromCodeCoverage]
    public sealed record ExpectedProduct(
        string Name,
        string? Barcode,
        string? Brand,
        string ProductType,
        string? Category,
        string? Description,
        string? Comment,
        string? ImageUrl,
        string BaseUnit,
        double BaseAmount,
        double DefaultPortionAmount,
        double CaloriesPerBase,
        double ProteinsPerBase,
        double FatsPerBase,
        double CarbsPerBase,
        double FiberPerBase,
        double AlcoholPerBase,
        string Visibility);
}
