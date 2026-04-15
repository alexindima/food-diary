using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using FoodDiary.Presentation.Api.Authorization;
using FoodDiary.Presentation.Api.Features.Auth.Requests;
using FoodDiary.Presentation.Api.Features.Cycles.Requests;
using FoodDiary.Presentation.Api.Features.Hydration.Requests;
using FoodDiary.Presentation.Api.Features.Images.Requests;
using FoodDiary.Presentation.Api.Features.Products.Requests;
using FoodDiary.Presentation.Api.Features.Recipes.Requests;
using FoodDiary.Presentation.Api.Features.ShoppingLists.Requests;
using FoodDiary.Presentation.Api.Features.Users.Requests;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

namespace FoodDiary.Web.Api.IntegrationTests;

public sealed class PresentationPayloadContractIntegrationTests(
    ApiWebApplicationFactory apiFactory,
    TestAuthApiWebApplicationFactory testAuthFactory)
    : IClassFixture<ApiWebApplicationFactory>, IClassFixture<TestAuthApiWebApplicationFactory> {
    private static readonly JsonSerializerOptions JsonOptions = new() {
        PropertyNameCaseInsensitive = true,
    };

    [Fact]
    public async Task AdminUsers_WithAdminRole_MatchesNormalizedPayloadSnapshot() {
        var client = testAuthFactory.CreateClient();
        var registeredEmail = $"admin-users-{Guid.NewGuid():N}@example.com";

        var registerResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterHttpRequest(registeredEmail, "Password123!", "en"));
        registerResponse.EnsureSuccessStatusCode();

        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.AuthenticateHeader, "true");
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, PresentationRoleNames.Admin);

        var response = await client.GetAsync("/api/v1/admin/users");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var actual = JsonSerializer.Serialize(
            BuildAdminUsersSnapshot(json.RootElement),
            new JsonSerializerOptions { WriteIndented = true });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await AssertPayloadSnapshotAsync("admin-users-list", actual);
    }

    [Fact]
    public async Task RecipeById_AfterCreate_MatchesNormalizedPayloadSnapshot() {
        var client = apiFactory.CreateClient();
        var accessToken = await RegisterAndGetAccessTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var productResponse = await client.PostAsJsonAsync(
            "/api/v1/products",
            new CreateProductHttpRequest(
                null,
                "Recipe Contract Product",
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
        Assert.Equal(HttpStatusCode.Created, productResponse.StatusCode);
        Assert.NotNull(productResponse.Headers.Location);

        using var productJson = JsonDocument.Parse(await productResponse.Content.ReadAsStringAsync());
        var productId = productJson.RootElement.GetProperty("id").GetGuid();

        var createRequest = new CreateRecipeHttpRequest(
            "Integration Recipe",
            "Contract test recipe",
            null,
            "Dinner",
            null,
            null,
            10,
            20,
            2,
            "private",
            false,
            420,
            25,
            15,
            30,
            5,
            0,
            [
                new RecipeStepHttpRequest(
                    "Prepare",
                    "Mix all ingredients.",
                    [
                        new RecipeIngredientHttpRequest(productId, null, 2.5)
                    ],
                    null,
                    null)
            ]);

        var createResponse = await client.PostAsJsonAsync("/api/v1/recipes", createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.NotNull(createResponse.Headers.Location);

        using var createdJson = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var recipeId = createdJson.RootElement.GetProperty("id").GetGuid();

        var getResponse = await client.GetAsync($"/api/v1/recipes/{recipeId}");
        using var recipeJson = JsonDocument.Parse(await getResponse.Content.ReadAsStringAsync());
        var actual = JsonSerializer.Serialize(
            BuildRecipeSnapshot(recipeJson.RootElement),
            new JsonSerializerOptions { WriteIndented = true });

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        await AssertPayloadSnapshotAsync("recipe-by-id", actual);
    }

    [Fact]
    public async Task Statistics_ForEmptyRange_MatchesNormalizedPayloadSnapshot() {
        var client = apiFactory.CreateClient();
        var accessToken = await RegisterAndGetAccessTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.GetAsync("/api/v1/statistics?dateFrom=2026-03-01&dateTo=2026-03-07&quantizationDays=1");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var actual = JsonSerializer.Serialize(
            BuildStatisticsSnapshot(json.RootElement),
            new JsonSerializerOptions { WriteIndented = true });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await AssertPayloadSnapshotAsync("statistics-empty-range", actual);
    }

    [Fact]
    public async Task ShoppingListCurrent_AfterCreate_MatchesNormalizedPayloadSnapshot() {
        var client = apiFactory.CreateClient();
        var accessToken = await RegisterAndGetAccessTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/shopping-lists",
            new CreateShoppingListHttpRequest(
                "Weekend Shopping",
                [
                    new ShoppingListItemHttpRequest(
                        null,
                        "Milk",
                        2,
                        "Ml",
                        "Dairy",
                        false,
                        1)
                ]));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.NotNull(createResponse.Headers.Location);

        var currentResponse = await client.GetAsync("/api/v1/shopping-lists/current");
        using var json = JsonDocument.Parse(await currentResponse.Content.ReadAsStringAsync());
        var actual = JsonSerializer.Serialize(
            BuildShoppingListSnapshot(json.RootElement),
            new JsonSerializerOptions { WriteIndented = true });

        Assert.Equal(HttpStatusCode.OK, currentResponse.StatusCode);
        await AssertPayloadSnapshotAsync("shopping-list-current", actual);
    }

    [Fact]
    public async Task CurrentCycle_AfterCreate_MatchesNormalizedPayloadSnapshot() {
        var client = apiFactory.CreateClient();
        var accessToken = await RegisterAndGetAccessTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/cycles",
            new CreateCycleHttpRequest(
                new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc),
                28,
                14,
                "Integration cycle"));
        createResponse.EnsureSuccessStatusCode();

        var currentResponse = await client.GetAsync("/api/v1/cycles/current");
        using var json = JsonDocument.Parse(await currentResponse.Content.ReadAsStringAsync());
        var actual = JsonSerializer.Serialize(
            BuildCycleSnapshot(json.RootElement),
            new JsonSerializerOptions { WriteIndented = true });

        Assert.Equal(HttpStatusCode.OK, currentResponse.StatusCode);
        await AssertPayloadSnapshotAsync("cycle-current", actual);
    }

    [Fact]
    public async Task DashboardSnapshot_ForNewUser_MatchesNormalizedPayloadSnapshot() {
        var client = apiFactory.CreateClient();
        var accessToken = await RegisterAndGetAccessTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.GetAsync("/api/v1/dashboard?date=2026-03-26&page=1&pageSize=10&locale=en&trendDays=7");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var actual = JsonSerializer.Serialize(
            BuildDashboardSnapshot(json.RootElement),
            new JsonSerializerOptions { WriteIndented = true });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await AssertPayloadSnapshotAsync("dashboard-snapshot", actual);
    }

    [Fact]
    public async Task UserInfo_AfterRegister_MatchesNormalizedPayloadSnapshot() {
        var client = apiFactory.CreateClient();
        var accessToken = await RegisterAndGetAccessTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.GetAsync("/api/v1/users/info");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var actual = JsonSerializer.Serialize(
            BuildUserInfoSnapshot(json.RootElement),
            new JsonSerializerOptions { WriteIndented = true });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await AssertPayloadSnapshotAsync("user-info", actual);
    }

    [Fact]
    public async Task UserOverview_AfterRegister_MatchesNormalizedPayloadSnapshot() {
        var client = apiFactory.CreateClient();
        var accessToken = await RegisterAndGetAccessTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.GetAsync("/api/v1/users/overview");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var actual = JsonSerializer.Serialize(
            BuildUserOverviewSnapshot(json.RootElement),
            new JsonSerializerOptions { WriteIndented = true });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await AssertPayloadSnapshotAsync("user-overview", actual);
    }

    [Fact]
    public async Task HydrationDaily_AfterCreate_MatchesNormalizedPayloadSnapshot() {
        var client = apiFactory.CreateClient();
        var accessToken = await RegisterAndGetAccessTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/hydrations",
            new CreateHydrationEntryHttpRequest(
                new DateTime(2026, 3, 26, 9, 30, 0, DateTimeKind.Utc),
                450));
        createResponse.EnsureSuccessStatusCode();

        var response = await client.GetAsync("/api/v1/hydrations/daily?dateUtc=2026-03-26");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var actual = JsonSerializer.Serialize(
            BuildHydrationDailySnapshot(json.RootElement),
            new JsonSerializerOptions { WriteIndented = true });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await AssertPayloadSnapshotAsync("hydration-daily", actual);
    }

    [Fact]
    public async Task AiUsageMe_WithAuthenticatedUser_MatchesNormalizedPayloadSnapshot() {
        var client = apiFactory.CreateClient();
        var accessToken = await RegisterAndGetAccessTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.GetAsync("/api/v1/ai/usage/me");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var actual = JsonSerializer.Serialize(
            BuildAiUsageSnapshot(json.RootElement),
            new JsonSerializerOptions { WriteIndented = true });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await AssertPayloadSnapshotAsync("ai-usage-me", actual);
    }

    [Fact]
    public async Task DesiredWeight_AfterUpdate_MatchesNormalizedPayloadSnapshot() {
        var client = apiFactory.CreateClient();
        var accessToken = await RegisterAndGetAccessTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var updateResponse = await client.PutAsJsonAsync(
            "/api/v1/users/desired-weight",
            new UpdateDesiredWeightHttpRequest(72.5));
        updateResponse.EnsureSuccessStatusCode();

        var response = await client.GetAsync("/api/v1/users/desired-weight");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var actual = JsonSerializer.Serialize(
            BuildDesiredMetricSnapshot(json.RootElement, "desiredWeight"),
            new JsonSerializerOptions { WriteIndented = true });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await AssertPayloadSnapshotAsync("desired-weight", actual);
    }

    [Fact]
    public async Task DesiredWaist_AfterUpdate_MatchesNormalizedPayloadSnapshot() {
        var client = apiFactory.CreateClient();
        var accessToken = await RegisterAndGetAccessTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var updateResponse = await client.PutAsJsonAsync(
            "/api/v1/users/desired-waist",
            new UpdateDesiredWaistHttpRequest(81.5));
        updateResponse.EnsureSuccessStatusCode();

        var response = await client.GetAsync("/api/v1/users/desired-waist");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var actual = JsonSerializer.Serialize(
            BuildDesiredMetricSnapshot(json.RootElement, "desiredWaist"),
            new JsonSerializerOptions { WriteIndented = true });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await AssertPayloadSnapshotAsync("desired-waist", actual);
    }

    [Fact]
    public async Task ImageUploadUrl_WithValidPayload_MatchesNormalizedPayloadSnapshot() {
        var client = testAuthFactory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.AuthenticateHeader, "true");
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, Guid.NewGuid().ToString());

        var response = await client.PostAsJsonAsync(
            "/api/v1/images/upload-url",
            new GetImageUploadUrlHttpRequest("payload-photo.jpg", "image/jpeg", 4096));
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var actual = JsonSerializer.Serialize(
            BuildImageUploadUrlSnapshot(json.RootElement),
            new JsonSerializerOptions { WriteIndented = true });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await AssertPayloadSnapshotAsync("image-upload-url", actual);
    }

    private static async Task<string> RegisterAndGetAccessTokenAsync(HttpClient client) {
        var email = $"api-tests-{Guid.NewGuid():N}@example.com";
        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterHttpRequest(email, "Password123!", "en"));
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<AuthPayload>(JsonOptions);
        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload.AccessToken));
        return payload.AccessToken;
    }

    private static JsonObject BuildAdminUsersSnapshot(JsonElement root) {
        var data = root.GetProperty("data");
        var firstUserKeys = data.GetArrayLength() > 0
            ? data[0].EnumerateObject().Select(property => property.Name).OrderBy(static name => name, StringComparer.Ordinal).ToArray()
            : Array.Empty<string>();

        return new JsonObject {
            ["keys"] = ToJsonArray(root.EnumerateObject().Select(property => property.Name).OrderBy(static name => name, StringComparer.Ordinal)),
            ["dataCount"] = data.GetArrayLength(),
            ["firstUserKeys"] = ToJsonArray(firstUserKeys)
        };
    }

    private static JsonObject BuildRecipeSnapshot(JsonElement root) {
        var steps = root.GetProperty("steps");
        var firstStep = steps[0];
        var ingredients = firstStep.GetProperty("ingredients");
        var firstIngredientKeys = ingredients[0].EnumerateObject()
            .Select(property => property.Name)
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToArray();

        return new JsonObject {
            ["keys"] = ToJsonArray(root.EnumerateObject().Select(property => property.Name).OrderBy(static name => name, StringComparer.Ordinal)),
            ["name"] = root.GetProperty("name").GetString(),
            ["servings"] = root.GetProperty("servings").GetInt32(),
            ["visibility"] = root.GetProperty("visibility").GetString(),
            ["isOwnedByCurrentUser"] = root.GetProperty("isOwnedByCurrentUser").GetBoolean(),
            ["stepsCount"] = steps.GetArrayLength(),
            ["stepKeys"] = ToJsonArray(firstStep.EnumerateObject().Select(property => property.Name).OrderBy(static name => name, StringComparer.Ordinal)),
            ["firstIngredientKeys"] = ToJsonArray(firstIngredientKeys)
        };
    }

    private static JsonObject BuildStatisticsSnapshot(JsonElement root) {
        return new JsonObject {
            ["isArray"] = root.ValueKind == JsonValueKind.Array,
            ["count"] = root.GetArrayLength()
        };
    }

    private static JsonObject BuildShoppingListSnapshot(JsonElement root) {
        var items = root.GetProperty("items");
        var firstItem = items[0];

        return new JsonObject {
            ["keys"] = ToJsonArray(root.EnumerateObject().Select(property => property.Name).OrderBy(static name => name, StringComparer.Ordinal)),
            ["name"] = root.GetProperty("name").GetString(),
            ["itemsCount"] = items.GetArrayLength(),
            ["itemKeys"] = ToJsonArray(firstItem.EnumerateObject().Select(property => property.Name).OrderBy(static name => name, StringComparer.Ordinal))
        };
    }

    private static JsonObject BuildCycleSnapshot(JsonElement root) {
        var predictions = root.GetProperty("predictions");

        return new JsonObject {
            ["keys"] = ToJsonArray(root.EnumerateObject().Select(property => property.Name).OrderBy(static name => name, StringComparer.Ordinal)),
            ["averageLength"] = root.GetProperty("averageLength").GetInt32(),
            ["lutealLength"] = root.GetProperty("lutealLength").GetInt32(),
            ["daysCount"] = root.GetProperty("days").GetArrayLength(),
            ["predictionKeys"] = ToJsonArray(predictions.EnumerateObject().Select(property => property.Name).OrderBy(static name => name, StringComparer.Ordinal))
        };
    }

    private static JsonObject BuildDashboardSnapshot(JsonElement root) {
        return new JsonObject {
            ["keys"] = ToJsonArray(root.EnumerateObject().Select(property => property.Name).OrderBy(static name => name, StringComparer.Ordinal)),
            ["weeklyCaloriesCount"] = root.GetProperty("weeklyCalories").GetArrayLength(),
            ["statisticsKeys"] = ToJsonArray(root.GetProperty("statistics").EnumerateObject().Select(property => property.Name).OrderBy(static name => name, StringComparer.Ordinal)),
            ["mealsKeys"] = ToJsonArray(root.GetProperty("meals").EnumerateObject().Select(property => property.Name).OrderBy(static name => name, StringComparer.Ordinal)),
            ["weightKeys"] = ToJsonArray(root.GetProperty("weight").EnumerateObject().Select(property => property.Name).OrderBy(static name => name, StringComparer.Ordinal)),
            ["waistKeys"] = ToJsonArray(root.GetProperty("waist").EnumerateObject().Select(property => property.Name).OrderBy(static name => name, StringComparer.Ordinal))
        };
    }

    private static JsonObject BuildUserInfoSnapshot(JsonElement root) {
        var dashboardLayout = root.GetProperty("dashboardLayout");
        var email = root.GetProperty("email").GetString() ?? string.Empty;

        return new JsonObject {
            ["keys"] = ToJsonArray(root.EnumerateObject().Select(property => property.Name).OrderBy(static name => name, StringComparer.Ordinal)),
            ["emailDomain"] = email.Contains('@', StringComparison.Ordinal) ? email.Split('@')[1] : string.Empty,
            ["language"] = root.TryGetProperty("language", out var language) ? language.GetString() : null,
            ["isActive"] = root.GetProperty("isActive").GetBoolean(),
            ["isEmailConfirmed"] = root.GetProperty("isEmailConfirmed").GetBoolean(),
            ["dashboardLayoutKeys"] = dashboardLayout.ValueKind == JsonValueKind.Object
                ? ToJsonArray(dashboardLayout.EnumerateObject().Select(property => property.Name).OrderBy(static name => name, StringComparer.Ordinal))
                : new JsonArray()
        };
    }

    private static JsonObject BuildUserOverviewSnapshot(JsonElement root) {
        var user = root.GetProperty("user");
        var notificationPreferences = root.GetProperty("notificationPreferences");
        var webPushSubscriptions = root.GetProperty("webPushSubscriptions");
        var dietologistRelationship = root.GetProperty("dietologistRelationship");

        return new JsonObject {
            ["keys"] = ToJsonArray(root.EnumerateObject().Select(property => property.Name).OrderBy(static name => name, StringComparer.Ordinal)),
            ["userKeys"] = ToJsonArray(user.EnumerateObject().Select(property => property.Name).OrderBy(static name => name, StringComparer.Ordinal)),
            ["notificationPreferenceKeys"] = ToJsonArray(
                notificationPreferences.EnumerateObject().Select(property => property.Name).OrderBy(static name => name, StringComparer.Ordinal)),
            ["webPushSubscriptionsCount"] = webPushSubscriptions.GetArrayLength(),
            ["dietologistRelationshipIsNull"] = dietologistRelationship.ValueKind == JsonValueKind.Null
        };
    }

    private static JsonObject BuildHydrationDailySnapshot(JsonElement root) {
        return new JsonObject {
            ["keys"] = ToJsonArray(root.EnumerateObject().Select(property => property.Name).OrderBy(static name => name, StringComparer.Ordinal)),
            ["dateUtc"] = root.GetProperty("dateUtc").GetDateTime().ToString("O"),
            ["totalMl"] = root.GetProperty("totalMl").GetInt32(),
            ["hasGoalMl"] = root.TryGetProperty("goalMl", out var goal) && goal.ValueKind is not JsonValueKind.Null
        };
    }

    private static JsonObject BuildAiUsageSnapshot(JsonElement root) {
        return new JsonObject {
            ["keys"] = ToJsonArray(root.EnumerateObject().Select(property => property.Name).OrderBy(static name => name, StringComparer.Ordinal)),
            ["inputLimit"] = root.GetProperty("inputLimit").GetInt64(),
            ["outputLimit"] = root.GetProperty("outputLimit").GetInt64(),
            ["inputUsed"] = root.GetProperty("inputUsed").GetInt64(),
            ["outputUsed"] = root.GetProperty("outputUsed").GetInt64(),
            ["hasResetAtUtc"] = root.TryGetProperty("resetAtUtc", out var resetAtUtc) && resetAtUtc.ValueKind == JsonValueKind.String
        };
    }

    private static JsonObject BuildDesiredMetricSnapshot(JsonElement root, string propertyName) {
        return new JsonObject {
            ["keys"] = ToJsonArray(root.EnumerateObject().Select(property => property.Name).OrderBy(static name => name, StringComparer.Ordinal)),
            [propertyName] = root.GetProperty(propertyName).GetDouble()
        };
    }

    private static JsonObject BuildImageUploadUrlSnapshot(JsonElement root) {
        var uploadUrl = root.GetProperty("uploadUrl").GetString() ?? string.Empty;
        var fileUrl = root.GetProperty("fileUrl").GetString() ?? string.Empty;
        var objectKey = root.GetProperty("objectKey").GetString() ?? string.Empty;
        var objectKeySegments = objectKey.Split('/', StringSplitOptions.RemoveEmptyEntries);

        return new JsonObject {
            ["keys"] = ToJsonArray(root.EnumerateObject().Select(property => property.Name).OrderBy(static name => name, StringComparer.Ordinal)),
            ["uploadUrlHost"] = Uri.TryCreate(uploadUrl, UriKind.Absolute, out var uploadUri) ? uploadUri.Host : string.Empty,
            ["uploadUrlHasSignature"] = uploadUrl.Contains("X-Amz-Signature=", StringComparison.Ordinal),
            ["fileUrlHost"] = Uri.TryCreate(fileUrl, UriKind.Absolute, out var fileUri) ? fileUri.Host : string.Empty,
            ["objectKeySegmentCount"] = objectKeySegments.Length,
            ["objectKeyStartsWithUsers"] = objectKey.StartsWith("users/", StringComparison.Ordinal),
            ["objectKeyContainsImagesSegment"] = objectKeySegments.Contains("images", StringComparer.Ordinal),
            ["assetIdIsGuid"] = root.GetProperty("assetId").GetGuid() != Guid.Empty
        };
    }

    private static JsonArray ToJsonArray(IEnumerable<string> values) {
        var array = new JsonArray();
        foreach (var value in values) {
            array.Add(value);
        }

        return array;
    }

    private static async Task AssertPayloadSnapshotAsync(string scenario, string actual) {
        var snapshotPath = SnapshotPathResolver.GetPath("payload-contract-snapshots.json");
        var snapshotRoot = JsonNode.Parse(await File.ReadAllTextAsync(snapshotPath))!.AsObject();
        if (string.Equals(Environment.GetEnvironmentVariable("UPDATE_CONTRACT_SNAPSHOTS"), "1", StringComparison.Ordinal)) {
            snapshotRoot[scenario] = JsonNode.Parse(actual);
            await File.WriteAllTextAsync(
                snapshotPath,
                snapshotRoot.ToJsonString(new JsonSerializerOptions { WriteIndented = true }).ReplaceLineEndings("\n"));
        }

        var expected = snapshotRoot[scenario]?.ToJsonString(new JsonSerializerOptions { WriteIndented = true });

        Assert.NotNull(expected);
        Assert.Equal(
            expected.ReplaceLineEndings("\n").TrimEnd(),
            actual.ReplaceLineEndings("\n").TrimEnd());
    }

    private sealed record AuthPayload(string AccessToken);
}
