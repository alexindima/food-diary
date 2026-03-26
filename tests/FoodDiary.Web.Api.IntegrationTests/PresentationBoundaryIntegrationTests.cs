using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using FoodDiary.Presentation.Api.Authorization;
using FoodDiary.Presentation.Api.Features.Ai.Requests;
using FoodDiary.Presentation.Api.Features.Auth.Requests;
using FoodDiary.Presentation.Api.Features.Images.Requests;
using FoodDiary.Presentation.Api.Features.Users.Requests;
using FoodDiary.Presentation.Api.Features.WaistEntries.Requests;
using FoodDiary.Presentation.Api.Features.WeightEntries.Requests;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

namespace FoodDiary.Web.Api.IntegrationTests;

public sealed class PresentationBoundaryIntegrationTests(
    ApiWebApplicationFactory apiFactory,
    TestAuthApiWebApplicationFactory testAuthFactory)
    : IClassFixture<ApiWebApplicationFactory>, IClassFixture<TestAuthApiWebApplicationFactory> {
    private static readonly JsonSerializerOptions JsonOptions = new() {
        PropertyNameCaseInsensitive = true,
    };
    private static readonly Guid MissingProductId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public async Task UsersInfo_WithAuthenticatedPrincipalMissingUserIdClaim_ReturnsUnauthorized() {
        var client = testAuthFactory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.AuthenticateHeader, "true");

        var response = await client.GetAsync("/api/users/info");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithInvalidEmail_ReturnsValidationErrorContract() {
        var client = apiFactory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterHttpRequest("not-an-email", "Password123!", "en"));
        var payload = await response.Content.ReadFromJsonAsync<ErrorPayload>(JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("Validation.Invalid", payload.Error);
        Assert.Equal("Invalid email format", payload.Message);
        Assert.NotNull(payload.Errors);
        Assert.Contains(payload.Errors.Keys, key => string.Equals(key, "email", StringComparison.Ordinal));
        await AssertErrorContractSnapshotAsync("register-invalid-email", payload);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsConflictContract() {
        var client = apiFactory.CreateClient();
        var email = $"api-tests-{Guid.NewGuid():N}@example.com";

        var firstResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterHttpRequest(email, "Password123!", "en"));
        firstResponse.EnsureSuccessStatusCode();

        var duplicateResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterHttpRequest(email, "Password123!", "en"));
        var payload = await duplicateResponse.Content.ReadFromJsonAsync<ErrorPayload>(JsonOptions);

        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("Validation.Conflict", payload.Error);
        Assert.NotNull(payload.Errors);
        Assert.Contains(payload.Errors.Keys, key => string.Equals(key, "email", StringComparison.Ordinal));
        await AssertErrorContractSnapshotAsync("register-duplicate-email", payload);
    }

    [Fact]
    public async Task Login_WhenRateLimitExceeded_ReturnsTooManyRequestsContract() {
        var client = apiFactory.CreateClient();
        var request = new LoginHttpRequest("missing-user@example.com", "Password123!");
        HttpResponseMessage? lastResponse = null;

        for (var i = 0; i < 6; i++) {
            lastResponse = await client.PostAsJsonAsync("/api/auth/login", request);
        }

        var payload = await lastResponse!.Content.ReadFromJsonAsync<ErrorPayload>(JsonOptions);

        Assert.Equal(HttpStatusCode.TooManyRequests, lastResponse.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("RateLimit.Exceeded", payload.Error);
    }

    [Fact]
    public async Task AdminDashboard_WithAuthenticatedNonAdminUser_ReturnsForbidden() {
        var client = testAuthFactory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.AuthenticateHeader, "true");
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, Guid.NewGuid().ToString());

        var response = await client.GetAsync("/api/admin/dashboard");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminDashboard_WithAdminRole_ReturnsOk() {
        var client = testAuthFactory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.AuthenticateHeader, "true");
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, PresentationRoleNames.Admin);

        var response = await client.GetAsync("/api/admin/dashboard");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetProductById_WithMissingProduct_ReturnsNotFoundContract() {
        var client = apiFactory.CreateClient();
        var accessToken = await RegisterAndGetAccessTokenAsync(client);
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.GetAsync($"/api/products/{MissingProductId}");
        var payload = await response.Content.ReadFromJsonAsync<ErrorPayload>(JsonOptions);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("Product.NotAccessible", payload.Error);
        await AssertErrorContractSnapshotAsync("products-missing-by-id", payload);
    }

    [Fact]
    public async Task CreateWeightEntry_WithDuplicateDate_ReturnsConflictContract() {
        var client = apiFactory.CreateClient();
        var accessToken = await RegisterAndGetAccessTokenAsync(client);
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var request = new CreateWeightEntryHttpRequest(
            new DateTime(2026, 3, 25, 12, 0, 0, DateTimeKind.Utc),
            80.5);

        var firstResponse = await client.PostAsJsonAsync("/api/weight-entries", request);
        firstResponse.EnsureSuccessStatusCode();

        var duplicateResponse = await client.PostAsJsonAsync("/api/weight-entries", request);
        var payload = await duplicateResponse.Content.ReadFromJsonAsync<ErrorPayload>(JsonOptions);

        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("WeightEntry.AlreadyExists", payload.Error);
        await AssertErrorContractSnapshotAsync("weight-entry-duplicate-date", payload);
    }

    [Fact]
    public async Task CreateWaistEntry_WithDuplicateDate_ReturnsConflictContract() {
        var client = apiFactory.CreateClient();
        var accessToken = await RegisterAndGetAccessTokenAsync(client);
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var request = new CreateWaistEntryHttpRequest(
            new DateTime(2026, 3, 25, 12, 0, 0, DateTimeKind.Utc),
            72.3);

        var firstResponse = await client.PostAsJsonAsync("/api/waist-entries", request);
        firstResponse.EnsureSuccessStatusCode();

        var duplicateResponse = await client.PostAsJsonAsync("/api/waist-entries", request);
        var payload = await duplicateResponse.Content.ReadFromJsonAsync<ErrorPayload>(JsonOptions);

        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("WaistEntry.AlreadyExists", payload.Error);
        await AssertErrorContractSnapshotAsync("waist-entry-duplicate-date", payload);
    }

    [Fact]
    public async Task CreateRecipe_WithInvalidBody_ReturnsValidationErrorContract() {
        var client = apiFactory.CreateClient();
        var accessToken = await RegisterAndGetAccessTokenAsync(client);
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.PostAsJsonAsync("/api/recipes", new { });
        var payload = await response.Content.ReadFromJsonAsync<ErrorPayload>(JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("Validation.Invalid", payload.Error);
        Assert.NotNull(payload.Errors);
        Assert.Contains(payload.Errors.Keys, key => string.Equals(key, "name", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ImageUploadUrl_WithInvalidPayload_ReturnsImageValidationContract() {
        var client = testAuthFactory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.AuthenticateHeader, "true");
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, Guid.NewGuid().ToString());

        var response = await client.PostAsJsonAsync(
            "/api/images/upload-url",
            new GetImageUploadUrlHttpRequest("photo.txt", "text/plain", 128));
        var payload = await response.Content.ReadFromJsonAsync<ErrorPayload>(JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("Image.InvalidData", payload.Error);
        Assert.Equal("Unsupported content type: text/plain.", payload.Message);
    }

    [Fact]
    public async Task DeleteImageAsset_AfterUploadUrl_ReturnsNoContent() {
        var client = testAuthFactory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.AuthenticateHeader, "true");
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, Guid.NewGuid().ToString());

        var uploadResponse = await client.PostAsJsonAsync(
            "/api/images/upload-url",
            new GetImageUploadUrlHttpRequest("photo.jpg", "image/jpeg", 1024));
        uploadResponse.EnsureSuccessStatusCode();

        using var uploadJson = JsonDocument.Parse(await uploadResponse.Content.ReadAsStringAsync());
        var assetId = uploadJson.RootElement.GetProperty("assetId").GetGuid();

        var deleteResponse = await client.DeleteAsync($"/api/images/{assetId}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteImageAsset_WithMissingAsset_ReturnsNotFoundContract() {
        var client = testAuthFactory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.AuthenticateHeader, "true");
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, Guid.NewGuid().ToString());

        var missingAssetId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var response = await client.DeleteAsync($"/api/images/{missingAssetId}");
        var payload = await response.Content.ReadFromJsonAsync<ErrorPayload>(JsonOptions);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("Image.NotFound", payload.Error);
    }

    [Fact]
    public async Task AiNutrition_WithEmptyItems_ReturnsValidationContract() {
        var client = testAuthFactory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.AuthenticateHeader, "true");
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, PresentationRoleNames.Premium);

        var response = await client.PostAsJsonAsync(
            "/api/ai/food/nutrition",
            new FoodNutritionHttpRequest([]));
        var payload = await response.Content.ReadFromJsonAsync<ErrorPayload>(JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("Validation.Required", payload.Error);
        Assert.NotNull(payload.Errors);
        Assert.Contains(payload.Errors.Keys, key => string.Equals(key, "items", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Statistics_WithInvalidDateRangeQuery_ReturnsValidationErrorContract() {
        var client = apiFactory.CreateClient();
        var accessToken = await RegisterAndGetAccessTokenAsync(client);
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.GetAsync("/api/statistics?dateFrom=invalid&dateTo=invalid");
        var payload = await response.Content.ReadFromJsonAsync<ErrorPayload>(JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("Validation.Invalid", payload.Error);
        Assert.NotNull(payload.Errors);
        Assert.Contains(payload.Errors.Keys, key => string.Equals(key, "dateFrom", StringComparison.Ordinal));
        Assert.Contains(payload.Errors.Keys, key => string.Equals(key, "dateTo", StringComparison.Ordinal));
    }

    [Fact]
    public async Task UpdateDesiredWeight_WithInvalidValue_ReturnsValidationErrorContract() {
        var client = apiFactory.CreateClient();
        var accessToken = await RegisterAndGetAccessTokenAsync(client);
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.PutAsJsonAsync(
            "/api/users/desired-weight",
            new UpdateDesiredWeightHttpRequest(-1));
        var payload = await response.Content.ReadFromJsonAsync<ErrorPayload>(JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("Validation.Invalid", payload.Error);
        Assert.NotNull(payload.Errors);
        Assert.Contains(payload.Errors.Keys, key => string.Equals(key, "desiredWeight", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SwaggerJson_ContainsExpectedPresentationRoutes() {
        var client = apiFactory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var paths = json.RootElement.GetProperty("paths");
        var pathNames = paths.EnumerateObject()
            .Select(property => property.Name)
            .ToArray();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(pathNames, path => string.Equals(path, "/api/products", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(pathNames, path => string.Equals(path, "/api/auth/register", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(pathNames, path => string.Equals(path, "/api/recipes", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(pathNames, path => string.Equals(path, "/api/statistics", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(pathNames, path => string.Equals(path, "/api/admin/dashboard", StringComparison.OrdinalIgnoreCase));
        Assert.True(json.RootElement.TryGetProperty("openapi", out _));
    }

    [Fact]
    public async Task SwaggerJson_ContainsBearerSecurityScheme() {
        var client = apiFactory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var securitySchemes = json.RootElement
            .GetProperty("components")
            .GetProperty("securitySchemes")
            .GetProperty("Bearer");
        var securityRequirement = json.RootElement
            .GetProperty("security")[0]
            .GetProperty("Bearer");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("http", securitySchemes.GetProperty("type").GetString());
        Assert.Equal("bearer", securitySchemes.GetProperty("scheme").GetString());
        Assert.Equal("JWT", securitySchemes.GetProperty("bearerFormat").GetString());
        Assert.Equal(JsonValueKind.Array, securityRequirement.ValueKind);
    }

    [Fact]
    public async Task SwaggerJson_MatchesFocusedPresentationContractSnapshot() {
        var client = apiFactory.CreateClient();
        var response = await client.GetAsync("/swagger/v1/swagger.json");
        response.EnsureSuccessStatusCode();

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var actual = BuildFocusedOpenApiSnapshot(json.RootElement);
        await AssertSnapshotAsync("openapi-focused-contract.json", actual);
    }

    [Fact]
    public async Task SwaggerJson_MatchesAuthAdminContractSnapshot() {
        var client = apiFactory.CreateClient();
        var response = await client.GetAsync("/swagger/v1/swagger.json");
        response.EnsureSuccessStatusCode();

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var actual = BuildAuthAdminOpenApiSnapshot(json.RootElement);
        await AssertSnapshotAsync("openapi-auth-admin-contract.json", actual);
    }

    [Fact]
    public async Task SwaggerJson_MatchesFullPresentationContractSnapshot() {
        var client = apiFactory.CreateClient();
        var response = await client.GetAsync("/swagger/v1/swagger.json");
        response.EnsureSuccessStatusCode();

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var actual = BuildFullOpenApiSnapshot(json.RootElement);
        await AssertSnapshotAsync("openapi-full-contract.json", actual);
    }

    [Fact]
    public async Task EmailVerificationHub_Negotiate_RequiresAuthentication() {
        var client = apiFactory.CreateClient();

        var response = await client.PostAsync("/hubs/email-verification/negotiate?negotiateVersion=1", content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task EmailVerificationHub_Negotiate_WithAccessTokenQuery_ReturnsConnectionInfo() {
        var client = apiFactory.CreateClient();
        var accessToken = await RegisterAndGetAccessTokenAsync(client);

        var response = await client.PostAsync($"/hubs/email-verification/negotiate?negotiateVersion=1&access_token={accessToken}", content: null);
        var payload = await response.Content.ReadFromJsonAsync<NegotiatePayload>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload.ConnectionId));
        Assert.False(string.IsNullOrWhiteSpace(payload.ConnectionToken));
    }

    [Fact]
    public async Task UnhandledException_ReturnsStandardErrorContractWithTraceId() {
        var client = apiFactory.CreateClient();

        var response = await client.GetAsync("/test/exceptions/unhandled");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("Server.Unexpected", root.GetProperty("error").GetString());
        Assert.Equal("An unexpected error occurred.", root.GetProperty("message").GetString());
        Assert.True(root.TryGetProperty("traceId", out var traceIdProperty));
        Assert.False(string.IsNullOrWhiteSpace(traceIdProperty.GetString()));
    }

    private static async Task<string> RegisterAndGetAccessTokenAsync(HttpClient client) {
        var email = $"api-tests-{Guid.NewGuid():N}@example.com";
        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterHttpRequest(email, "Password123!", "en"));
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<AuthPayload>(JsonOptions);
        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload.AccessToken));
        return payload.AccessToken;
    }

    private static string BuildFocusedOpenApiSnapshot(JsonElement root) {
        var selectedPaths = new[] {
            "/api/auth/register",
            "/api/auth/login",
            "/api/products",
            "/api/products/{id}",
            "/api/recipes",
            "/api/recipes/{id}",
            "/api/statistics",
            "/api/users/info",
            "/api/weight-entries",
            "/api/waist-entries"
        };

        var paths = root.GetProperty("paths");
        var endpoints = selectedPaths
            .Select(path => CreateEndpointSnapshot(paths, path))
            .OrderBy(endpoint => endpoint.Path, StringComparer.Ordinal)
            .ToArray();

        var snapshot = new OpenApiFocusedSnapshot(
            root.GetProperty("openapi").GetString() ?? string.Empty,
            endpoints);

        return JsonSerializer.Serialize(snapshot, new JsonSerializerOptions {
            WriteIndented = true
        });
    }

    private static string BuildAuthAdminOpenApiSnapshot(JsonElement root) {
        var selectedPaths = new[] {
            "/api/auth/register",
            "/api/auth/login",
            "/api/auth/refresh",
            "/api/auth/verify-email",
            "/api/auth/verify-email/resend",
            "/api/auth/admin-sso/start",
            "/api/auth/admin-sso/exchange",
            "/api/admin/dashboard",
            "/api/admin/users",
            "/api/admin/users/{id}",
            "/api/admin/email-templates",
            "/api/admin/email-templates/{key}/{locale}",
            "/api/admin/ai-usage/summary"
        };

        var paths = root.GetProperty("paths");
        var endpoints = selectedPaths
            .Select(path => CreateEndpointSnapshot(paths, path))
            .OrderBy(endpoint => endpoint.Path, StringComparer.Ordinal)
            .ToArray();

        var snapshot = new OpenApiFocusedSnapshot(
            root.GetProperty("openapi").GetString() ?? string.Empty,
            endpoints);

        return JsonSerializer.Serialize(snapshot, new JsonSerializerOptions {
            WriteIndented = true
        });
    }

    private static string BuildFullOpenApiSnapshot(JsonElement root) {
        var paths = root.GetProperty("paths");
        var endpoints = paths.EnumerateObject()
            .Select(property => CreateEndpointSnapshot(paths, property.Name))
            .OrderBy(endpoint => endpoint.Path, StringComparer.Ordinal)
            .ToArray();

        var snapshot = new OpenApiFocusedSnapshot(
            root.GetProperty("openapi").GetString() ?? string.Empty,
            endpoints);

        return JsonSerializer.Serialize(snapshot, new JsonSerializerOptions {
            WriteIndented = true
        });
    }

    private static EndpointSnapshot CreateEndpointSnapshot(JsonElement paths, string path) {
        var pathNode = paths.GetProperty(path);
        var operations = pathNode.EnumerateObject()
            .Select(operation => new OperationSnapshot(
                operation.Name,
                operation.Value.TryGetProperty("requestBody", out _),
                operation.Value.TryGetProperty("responses", out var responses)
                    ? responses.EnumerateObject()
                        .Select(response => response.Name)
                        .OrderBy(code => code, StringComparer.Ordinal)
                        .ToArray()
                    : Array.Empty<string>()))
            .OrderBy(operation => operation.Method, StringComparer.Ordinal)
            .ToArray();

        return new EndpointSnapshot(path.ToLowerInvariant(), operations);
    }

    private static async Task AssertErrorContractSnapshotAsync(string scenario, ErrorPayload payload) {
        var snapshotPath = SnapshotPathResolver.GetPath("error-contract-snapshots.json");
        var snapshotRoot = JsonNode.Parse(await File.ReadAllTextAsync(snapshotPath))!.AsObject();
        var serializerOptions = new JsonSerializerOptions {
            WriteIndented = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        };
        var expected = snapshotRoot[scenario]?.ToJsonString(serializerOptions);
        var actual = JsonSerializer.Serialize(
            new ErrorContractSnapshot(payload.Error, payload.Message, payload.Errors),
            serializerOptions);

        Assert.NotNull(expected);
        Assert.Equal(
            expected.ReplaceLineEndings("\n").TrimEnd(),
            actual.ReplaceLineEndings("\n").TrimEnd());
    }

    private static async Task AssertSnapshotAsync(string snapshotFileName, string actual) {
        var snapshotPath = SnapshotPathResolver.GetPath(snapshotFileName);
        if (string.Equals(Environment.GetEnvironmentVariable("UPDATE_CONTRACT_SNAPSHOTS"), "1", StringComparison.Ordinal)) {
            await File.WriteAllTextAsync(snapshotPath, actual.ReplaceLineEndings("\n"));
        }

        var expected = await File.ReadAllTextAsync(snapshotPath);
        Assert.Equal(
            expected.ReplaceLineEndings("\n").TrimEnd(),
            actual.ReplaceLineEndings("\n").TrimEnd());
    }

    private sealed record AuthPayload(string AccessToken);

    private sealed record ErrorPayload(string Error, string Message, string? TraceId = null, IReadOnlyDictionary<string, string[]>? Errors = null);

    private sealed record ErrorContractSnapshot(string Error, string Message, IReadOnlyDictionary<string, string[]>? Errors = null);

    private sealed record NegotiatePayload(string ConnectionId, string ConnectionToken);

    private sealed record OpenApiFocusedSnapshot(string OpenApi, IReadOnlyList<EndpointSnapshot> Endpoints);

    private sealed record EndpointSnapshot(string Path, IReadOnlyList<OperationSnapshot> Operations);

    private sealed record OperationSnapshot(string Method, bool HasRequestBody, IReadOnlyList<string> ResponseCodes);
}
