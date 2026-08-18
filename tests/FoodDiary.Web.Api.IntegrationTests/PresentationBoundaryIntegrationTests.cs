using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using FoodDiary.Presentation.Api.Authorization;
using FoodDiary.Presentation.Api.Features.Admin.Requests;
using FoodDiary.Presentation.Api.Features.Ai.Models;
using FoodDiary.Presentation.Api.Features.Ai.Requests;
using FoodDiary.Presentation.Api.Features.Auth.Requests;
using FoodDiary.Presentation.Api.Features.Images.Requests;
using FoodDiary.Presentation.Api.Features.Users.Requests;
using FoodDiary.Presentation.Api.Features.WaistEntries.Requests;
using FoodDiary.Presentation.Api.Features.WeightEntries.Requests;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class PresentationBoundaryIntegrationTests(
    ApiWebApplicationFactory apiFactory,
    TestAuthApiWebApplicationFactory testAuthFactory)
    : IClassFixture<ApiWebApplicationFactory>, IClassFixture<TestAuthApiWebApplicationFactory> {
    private static readonly JsonSerializerOptions JsonOptions = new() {
        PropertyNameCaseInsensitive = true,
    };
    private static readonly JsonSerializerOptions IndentedJsonOptions = new() {
        WriteIndented = true,
    };
    private static readonly JsonSerializerOptions ErrorSnapshotJsonOptions = new() {
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    private static readonly Guid MissingProductId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public async Task UsersInfo_WithAuthenticatedPrincipalMissingUserIdClaim_ReturnsUnauthorized() {
        HttpClient client = testAuthFactory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.AuthenticateHeader, "true");

        HttpResponseMessage response = await client.GetAsync("/api/v1/users/info");
        ErrorPayload? payload = await response.Content.ReadFromJsonAsync<ErrorPayload>(JsonOptions);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("Authentication.Unauthorized", payload.Error);
        Assert.False(string.IsNullOrWhiteSpace(payload.TraceId));
    }

    [Fact]
    public async Task Register_WithInvalidEmail_ReturnsValidationErrorContract() {
        HttpClient client = apiFactory.CreateClient();
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterHttpRequest("not-an-email", "Password123!", "en"));
        ErrorPayload? payload = await response.Content.ReadFromJsonAsync<ErrorPayload>(JsonOptions);

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
        HttpClient client = apiFactory.CreateClient();
        string email = $"api-tests-{Guid.NewGuid():N}@example.com";

        HttpResponseMessage firstResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterHttpRequest(email, "Password123!", "en"));
        firstResponse.EnsureSuccessStatusCode();
        Assert.True(firstResponse.Headers.CacheControl?.NoStore);

        HttpResponseMessage duplicateResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterHttpRequest(email, "Password123!", "en"));
        ErrorPayload? payload = await duplicateResponse.Content.ReadFromJsonAsync<ErrorPayload>(JsonOptions);

        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("Validation.Conflict", payload.Error);
        Assert.NotNull(payload.Errors);
        Assert.Contains(payload.Errors.Keys, key => string.Equals(key, "email", StringComparison.Ordinal));
        await AssertErrorContractSnapshotAsync("register-duplicate-email", payload);
    }

    [Fact]
    public async Task Login_WhenRateLimitExceeded_ReturnsTooManyRequestsContract() {
        await using WebApplicationFactory<Program> limitedFactory = apiFactory.WithWebHostBuilder(builder => {
            builder.ConfigureAppConfiguration((_, configBuilder) => {
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
                    ["RateLimiting:Auth:PermitLimit"] = "5",
                    ["RateLimiting:Auth:WindowSeconds"] = "60",
                });
            });
        });
        HttpClient client = limitedFactory.CreateClient();
        var request = new LoginHttpRequest("missing-user@example.com", "Password123!");
        HttpResponseMessage? lastResponse = null;

        for (int i = 0; i < 6; i++) {
            lastResponse = await client.PostAsJsonAsync("/api/v1/auth/login", request);
        }

        ErrorPayload? payload = await lastResponse!.Content.ReadFromJsonAsync<ErrorPayload>(JsonOptions);

        Assert.Equal(HttpStatusCode.TooManyRequests, lastResponse.StatusCode);
        Assert.NotNull(lastResponse.Headers.RetryAfter);
        Assert.NotNull(payload);
        Assert.Equal("RateLimit.Exceeded", payload.Error);
        Assert.False(string.IsNullOrWhiteSpace(payload.TraceId));
    }

    [Fact]
    public async Task TestDeliveryRateLimit_IsPartitionedByAuthenticatedUser() {
        await using WebApplicationFactory<Program> limitedFactory = testAuthFactory.WithWebHostBuilder(builder => {
            builder.ConfigureAppConfiguration((_, configBuilder) => {
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
                    ["RateLimiting:TestDelivery:PermitLimit"] = "1",
                    ["RateLimiting:TestDelivery:WindowSeconds"] = "60",
                });
            });
        });
        HttpClient firstUserClient = CreateTestUserClient(limitedFactory, Guid.NewGuid());
        HttpClient secondUserClient = CreateTestUserClient(limitedFactory, Guid.NewGuid());

        HttpResponseMessage firstUserInitial = await firstUserClient.PostAsync("/api/v1/dashboard/test-email", content: null);
        HttpResponseMessage secondUserInitial = await secondUserClient.PostAsync("/api/v1/dashboard/test-email", content: null);
        HttpResponseMessage firstUserLimited = await firstUserClient.PostAsync("/api/v1/dashboard/test-email", content: null);
        ErrorPayload? payload = await firstUserLimited.Content.ReadFromJsonAsync<ErrorPayload>(JsonOptions);

        Assert.Multiple(
            () => Assert.NotEqual(HttpStatusCode.TooManyRequests, firstUserInitial.StatusCode),
            () => Assert.NotEqual(HttpStatusCode.TooManyRequests, secondUserInitial.StatusCode),
            () => Assert.Equal(HttpStatusCode.TooManyRequests, firstUserLimited.StatusCode),
            () => Assert.NotNull(firstUserLimited.Headers.RetryAfter),
            () => Assert.Equal("RateLimit.Exceeded", payload?.Error),
            () => Assert.False(string.IsNullOrWhiteSpace(payload?.TraceId)));
    }

    [Fact]
    public async Task TelegramBotAuth_WithoutConfiguredSecret_ReturnsInternalServerErrorContractWithTraceId() {
        HttpClient client = apiFactory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/auth/telegram/bot/auth",
            new TelegramBotAuthHttpRequest(123456789));
        ErrorPayload? payload = await response.Content.ReadFromJsonAsync<ErrorPayload>(JsonOptions);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("Authentication.TelegramBotNotConfigured", payload.Error);
        Assert.False(string.IsNullOrWhiteSpace(payload.TraceId));
    }

    [Fact]
    public async Task AdminDashboard_WithAuthenticatedNonAdminUser_ReturnsForbidden() {
        HttpClient client = testAuthFactory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.AuthenticateHeader, "true");
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, Guid.NewGuid().ToString());

        HttpResponseMessage response = await client.GetAsync("/api/v1/admin/dashboard");
        ErrorPayload? payload = await response.Content.ReadFromJsonAsync<ErrorPayload>(JsonOptions);

        Assert.NotNull(payload);
        Assert.Multiple(
            () => Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode),
            () => Assert.Equal("Authentication.Forbidden", payload.Error),
            () => Assert.Equal("You do not have permission to access this resource.", payload.Message),
            () => Assert.False(string.IsNullOrWhiteSpace(payload.TraceId)));
    }

    [Fact]
    public async Task AdminDashboard_WithAdminRole_ReturnsOk() {
        HttpClient client = testAuthFactory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.AuthenticateHeader, "true");
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, PresentationRoleNames.Admin);

        HttpResponseMessage response = await client.GetAsync("/api/v1/admin/dashboard");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AdminAchievementDefinitions_EnforcesAuthenticationAndAdminRole() {
        HttpClient anonymousClient = testAuthFactory.CreateClient();
        HttpResponseMessage anonymous = await anonymousClient.GetAsync("/api/v1/admin/achievement-definitions");
        ErrorPayload? anonymousPayload = await anonymous.Content.ReadFromJsonAsync<ErrorPayload>(JsonOptions);

        HttpClient userClient = testAuthFactory.CreateClient();
        userClient.DefaultRequestHeaders.Add(TestAuthenticationHandler.AuthenticateHeader, "true");
        userClient.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, Guid.NewGuid().ToString());
        HttpResponseMessage forbidden = await userClient.GetAsync("/api/v1/admin/achievement-definitions");
        ErrorPayload? forbiddenPayload = await forbidden.Content.ReadFromJsonAsync<ErrorPayload>(JsonOptions);

        Assert.NotNull(anonymousPayload);
        Assert.NotNull(forbiddenPayload);
        Assert.Multiple(
            () => Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode),
            () => Assert.Equal("Authentication.Unauthorized", anonymousPayload.Error),
            () => Assert.Equal("Authentication is required.", anonymousPayload.Message),
            () => Assert.False(string.IsNullOrWhiteSpace(anonymousPayload.TraceId)),
            () => Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode),
            () => Assert.Equal("Authentication.Forbidden", forbiddenPayload.Error),
            () => Assert.False(string.IsNullOrWhiteSpace(forbiddenPayload.TraceId)));
    }

    [Fact]
    public async Task AdminAchievementDefinitions_WithNullKey_ReturnsValidationContract() {
        HttpClient client = testAuthFactory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.AuthenticateHeader, "true");
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, PresentationRoleNames.Admin);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/admin/achievement-definitions",
            new {
                key = (string?)null,
                category = "habits",
                metric = "TotalMeals",
                threshold = 10,
                titleRu = "Название",
                titleEn = "Title",
                descriptionRu = "Описание",
                descriptionEn = "Description",
                icon = "trophy",
                sortOrder = 10,
                isActive = true,
            });
        ErrorPayload? payload = await response.Content.ReadFromJsonAsync<ErrorPayload>(JsonOptions);

        Assert.Multiple(
            () => Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode),
            () => Assert.NotNull(payload),
            () => Assert.Equal("Validation.Invalid", payload?.Error));
    }

    [Fact]
    public async Task AdminUserSetPassword_WithAdminRole_ReplacesUserPassword() {
        HttpClient client = testAuthFactory.CreateClient();
        string email = $"admin-password-{Guid.NewGuid():N}@example.com";
        const string oldPassword = "Password123!";
        const string newPassword = "NewPassword456!";

        HttpResponseMessage registerResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterHttpRequest(email, oldPassword, "en"));
        registerResponse.EnsureSuccessStatusCode();

        using var registerJson = JsonDocument.Parse(await registerResponse.Content.ReadAsStringAsync());
        Guid userId = registerJson.RootElement.GetProperty("user").GetProperty("id").GetGuid();

        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.AuthenticateHeader, "true");
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, PresentationRoleNames.Admin);

        HttpResponseMessage setPasswordResponse = await client.PatchAsJsonAsync(
            $"/api/v1/admin/users/{userId}/password",
            new AdminUserSetPasswordHttpRequest(newPassword));

        Assert.Equal(HttpStatusCode.NoContent, setPasswordResponse.StatusCode);

        client.DefaultRequestHeaders.Remove(TestAuthenticationHandler.AuthenticateHeader);
        client.DefaultRequestHeaders.Remove(TestAuthenticationHandler.UserIdHeader);
        client.DefaultRequestHeaders.Remove(TestAuthenticationHandler.RoleHeader);

        HttpResponseMessage oldLoginResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginHttpRequest(email, oldPassword));
        HttpResponseMessage newLoginResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginHttpRequest(email, newPassword));

        Assert.Equal(HttpStatusCode.Unauthorized, oldLoginResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, newLoginResponse.StatusCode);
    }

    [Fact]
    public async Task AdminLessonsImport_WithAdminRole_CreatesLessons() {
        HttpClient client = testAuthFactory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.AuthenticateHeader, "true");
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, PresentationRoleNames.Admin);
        string title = $"Imported lesson {Guid.NewGuid():N}";

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/admin/lessons/import",
            new AdminLessonsImportHttpRequest(
                Version: 1,
                Lessons: [
                    new AdminLessonImportItemHttpRequest(
                        Title: title,
                        Content: "<h2>Balanced plate</h2><p>Use a practical structure.</p>",
                        Summary: "A short practical lesson.",
                        Locale: "en",
                        Category: "NutritionBasics",
                        Difficulty: "Beginner",
                        EstimatedReadMinutes: 3,
                        SortOrder: 10),
                ]));
        using var importJson = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, importJson.RootElement.GetProperty("importedCount").GetInt32());
        Assert.Equal(title, importJson.RootElement.GetProperty("lessons")[0].GetProperty("title").GetString());

        HttpResponseMessage getResponse = await client.GetAsync("/api/v1/admin/lessons");
        using var listJson = JsonDocument.Parse(await getResponse.Content.ReadAsStringAsync());
        JsonElement importedLesson = listJson.RootElement.EnumerateArray()
            .SingleOrDefault(lesson => string.Equals(
                lesson.GetProperty("title").GetString(),
                title,
                StringComparison.Ordinal));

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.Equal(title, importedLesson.GetProperty("title").GetString());
        Assert.Equal("en", importedLesson.GetProperty("locale").GetString());
        Assert.Equal("NutritionBasics", importedLesson.GetProperty("category").GetString());
    }

    [Fact]
    public async Task ChangePassword_WithImpersonatedUser_ReturnsForbiddenErrorContract() {
        HttpClient client = testAuthFactory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.AuthenticateHeader, "true");
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.ImpersonationHeader, "true");
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.ImpersonationActorUserIdHeader, Guid.NewGuid().ToString());

        HttpResponseMessage response = await client.PatchAsJsonAsync(
            "/api/v1/users/password",
            new ChangePasswordHttpRequest("Password123!", "Password456!"));
        ErrorPayload? payload = await response.Content.ReadFromJsonAsync<ErrorPayload>(JsonOptions);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("Authentication.ImpersonationActionForbidden", payload.Error);
        Assert.False(string.IsNullOrWhiteSpace(payload.TraceId));
    }

    [Fact]
    public async Task GetProductById_WithMissingProduct_ReturnsNotFoundContract() {
        HttpClient client = apiFactory.CreateClient();
        string accessToken = await RegisterAndGetAccessTokenAsync(client);
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        HttpResponseMessage response = await client.GetAsync($"/api/v1/products/{MissingProductId}");
        ErrorPayload? payload = await response.Content.ReadFromJsonAsync<ErrorPayload>(JsonOptions);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("Product.NotAccessible", payload.Error);
        await AssertErrorContractSnapshotAsync("products-missing-by-id", payload);
    }

    [Fact]
    public async Task CreateWeightEntry_WithDuplicateDate_ReturnsConflictContract() {
        HttpClient client = apiFactory.CreateClient();
        string accessToken = await RegisterAndGetAccessTokenAsync(client);
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var request = new CreateWeightEntryHttpRequest(
            new DateTime(2026, 3, 25, 12, 0, 0, DateTimeKind.Utc),
            80.5);

        HttpResponseMessage firstResponse = await client.PostAsJsonAsync("/api/v1/weight-entries", request);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        HttpResponseMessage duplicateResponse = await client.PostAsJsonAsync("/api/v1/weight-entries", request);
        ErrorPayload? payload = await duplicateResponse.Content.ReadFromJsonAsync<ErrorPayload>(JsonOptions);

        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("WeightEntry.AlreadyExists", payload.Error);
        await AssertErrorContractSnapshotAsync("weight-entry-duplicate-date", payload);
    }

    [Fact]
    public async Task CreateWaistEntry_WithDuplicateDate_ReturnsConflictContract() {
        HttpClient client = apiFactory.CreateClient();
        string accessToken = await RegisterAndGetAccessTokenAsync(client);
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var request = new CreateWaistEntryHttpRequest(
            new DateTime(2026, 3, 25, 12, 0, 0, DateTimeKind.Utc),
            72.3);

        HttpResponseMessage firstResponse = await client.PostAsJsonAsync("/api/v1/waist-entries", request);
        firstResponse.EnsureSuccessStatusCode();

        HttpResponseMessage duplicateResponse = await client.PostAsJsonAsync("/api/v1/waist-entries", request);
        ErrorPayload? payload = await duplicateResponse.Content.ReadFromJsonAsync<ErrorPayload>(JsonOptions);

        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("WaistEntry.AlreadyExists", payload.Error);
        await AssertErrorContractSnapshotAsync("waist-entry-duplicate-date", payload);
    }

    [Fact]
    public async Task CreateRecipe_WithInvalidBody_ReturnsValidationErrorContract() {
        HttpClient client = apiFactory.CreateClient();
        string accessToken = await RegisterAndGetAccessTokenAsync(client);
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/recipes", new { });
        ErrorPayload? payload = await response.Content.ReadFromJsonAsync<ErrorPayload>(JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("Validation.Invalid", payload.Error);
        Assert.NotNull(payload.Errors);
        Assert.Contains(payload.Errors.Keys, key => string.Equals(key, "name", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ImageUploadUrl_WithInvalidPayload_ReturnsImageValidationContract() {
        HttpClient client = testAuthFactory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.AuthenticateHeader, "true");
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, Guid.NewGuid().ToString());

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/images/upload-url",
            new GetImageUploadUrlHttpRequest("photo.txt", "text/plain", 128));
        ErrorPayload? payload = await response.Content.ReadFromJsonAsync<ErrorPayload>(JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("Image.InvalidData", payload.Error);
        Assert.Equal("Unsupported content type: text/plain.", payload.Message);
    }

    [Fact]
    public async Task DeleteImageAsset_AfterUploadUrl_ReturnsNoContent() {
        HttpClient client = testAuthFactory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.AuthenticateHeader, "true");
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, Guid.NewGuid().ToString());

        HttpResponseMessage uploadResponse = await client.PostAsJsonAsync(
            "/api/v1/images/upload-url",
            new GetImageUploadUrlHttpRequest("photo.jpg", "image/jpeg", 1024));
        uploadResponse.EnsureSuccessStatusCode();

        using var uploadJson = JsonDocument.Parse(await uploadResponse.Content.ReadAsStringAsync());
        Guid assetId = uploadJson.RootElement.GetProperty("assetId").GetGuid();

        HttpResponseMessage deleteResponse = await client.DeleteAsync($"/api/v1/images/{assetId}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteImageAsset_WithMissingAsset_ReturnsNotFoundContract() {
        HttpClient client = testAuthFactory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.AuthenticateHeader, "true");
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, Guid.NewGuid().ToString());

        var missingAssetId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        HttpResponseMessage response = await client.DeleteAsync($"/api/v1/images/{missingAssetId}");
        ErrorPayload? payload = await response.Content.ReadFromJsonAsync<ErrorPayload>(JsonOptions);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("Image.NotFound", payload.Error);
    }

    [Fact]
    public async Task AiNutrition_WithEmptyItems_ReturnsValidationContract() {
        HttpClient client = testAuthFactory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.AuthenticateHeader, "true");
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, PresentationRoleNames.Premium);
        client.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString());

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/ai/food/nutrition",
            new FoodNutritionHttpRequest([]));
        ErrorPayload? payload = await response.Content.ReadFromJsonAsync<ErrorPayload>(JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("Validation.Required", payload.Error);
        Assert.NotNull(payload.Errors);
        Assert.Contains(payload.Errors.Keys, key => string.Equals(key, "items", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AiNutrition_WithoutIdempotencyKey_ReturnsRequiredContract() {
        HttpClient client = testAuthFactory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.AuthenticateHeader, "true");
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, PresentationRoleNames.Premium);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/ai/food/nutrition",
            new FoodNutritionHttpRequest([
                new FoodVisionItemHttpModel("egg", "egg", 2, "pcs", 0.9m),
            ]));
        ErrorPayload? payload = await response.Content.ReadFromJsonAsync<ErrorPayload>(JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("Idempotency.Required", payload.Error);
    }

    [Fact]
    public async Task Statistics_WithInvalidDateRangeQuery_ReturnsValidationErrorContract() {
        HttpClient client = apiFactory.CreateClient();
        string accessToken = await RegisterAndGetAccessTokenAsync(client);
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        HttpResponseMessage response = await client.GetAsync("/api/v1/statistics?dateFrom=invalid&dateTo=invalid");
        ErrorPayload? payload = await response.Content.ReadFromJsonAsync<ErrorPayload>(JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("Validation.Invalid", payload.Error);
        Assert.NotNull(payload.Errors);
        Assert.Contains(payload.Errors.Keys, key => string.Equals(key, "dateFrom", StringComparison.Ordinal));
        Assert.Contains(payload.Errors.Keys, key => string.Equals(key, "dateTo", StringComparison.Ordinal));
    }

    [Fact]
    public async Task UpdateDesiredWeight_WithInvalidValue_ReturnsValidationErrorContract() {
        HttpClient client = apiFactory.CreateClient();
        string accessToken = await RegisterAndGetAccessTokenAsync(client);
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        HttpResponseMessage response = await client.PutAsJsonAsync(
            "/api/v1/users/desired-weight",
            new UpdateDesiredWeightHttpRequest(-1));
        ErrorPayload? payload = await response.Content.ReadFromJsonAsync<ErrorPayload>(JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("Validation.Invalid", payload.Error);
        Assert.NotNull(payload.Errors);
        Assert.Contains(payload.Errors.Keys, key => string.Equals(key, "desiredWeightKg", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ApiVersion_ReturnsConfiguredBuildMetadata() {
        await using WebApplicationFactory<Program> versionedFactory = apiFactory.WithWebHostBuilder(builder => {
            builder.ConfigureAppConfiguration((_, configBuilder) => {
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
                    ["BuildInfo:CommitSha"] = "abc123def456",
                    ["BuildInfo:ImageTag"] = "sha-abc123def456",
                });
            });
        });
        HttpClient client = versionedFactory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/v1/version");
        ApiVersionPayload? payload = await response.Content.ReadFromJsonAsync<ApiVersionPayload>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("abc123def456", payload.CommitSha);
        Assert.Equal("sha-abc123def456", payload.ImageTag);
        Assert.Equal("Development", payload.Environment);
        Assert.False(string.IsNullOrWhiteSpace(payload.ApplicationVersion));
        Assert.True(payload.StartedAtUtc > DateTimeOffset.MinValue);
        Assert.NotNull(response.Headers.CacheControl);
        Assert.True(response.Headers.CacheControl.NoStore);
    }

    [Fact]
    public async Task SwaggerJson_ContainsExpectedPresentationRoutes() {
        HttpClient client = apiFactory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/swagger/v1/swagger.json");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        JsonElement paths = json.RootElement.GetProperty("paths");
        string[] pathNames = [.. paths.EnumerateObject().Select(property => property.Name)];

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(pathNames, path => string.Equals(path, "/api/v{version}/products", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(pathNames, path => string.Equals(path, "/api/v{version}/auth/register", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(pathNames, path => string.Equals(path, "/api/v{version}/recipes", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(pathNames, path => string.Equals(path, "/api/v{version}/statistics", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(pathNames, path => string.Equals(path, "/api/v{version}/admin/dashboard", StringComparison.OrdinalIgnoreCase));
        Assert.True(json.RootElement.TryGetProperty("openapi", out _));
    }

    [Fact]
    public async Task SwaggerJson_DeclaresBearerSecurityOnlyForAuthorizedOperations() {
        HttpClient client = apiFactory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/swagger/v1/swagger.json");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        JsonElement securitySchemes = json.RootElement
            .GetProperty("components")
            .GetProperty("securitySchemes")
            .GetProperty("Bearer");
        JsonElement authorizedSecurityRequirement = json.RootElement
            .GetProperty("paths")
            .GetProperty("/api/v{version}/products")
            .GetProperty("get")
            .GetProperty("security")[0]
            .GetProperty("Bearer");
        JsonElement anonymousSecurity = json.RootElement
            .GetProperty("paths")
            .GetProperty("/api/v{version}/auth/login")
            .GetProperty("post")
            .GetProperty("security");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("http", securitySchemes.GetProperty("type").GetString());
        Assert.Equal("bearer", securitySchemes.GetProperty("scheme").GetString());
        Assert.Equal("JWT", securitySchemes.GetProperty("bearerFormat").GetString());
        Assert.False(json.RootElement.TryGetProperty("security", out _));
        Assert.Equal(JsonValueKind.Array, authorizedSecurityRequirement.ValueKind);
        Assert.Equal(0, anonymousSecurity.GetArrayLength());
    }

    [Fact]
    public async Task SwaggerJson_HidesCurrentUserBindingAndDocumentsIdempotencyHeader() {
        HttpClient client = apiFactory.CreateClient();
        using var json = JsonDocument.Parse(await client.GetStringAsync("/swagger/v1/swagger.json"));
        JsonElement paths = json.RootElement.GetProperty("paths");

        JsonElement[] allParameters = [.. paths.EnumerateObject()
            .SelectMany(static path => path.Value.EnumerateObject())
            .Where(static operation => operation.Value.ValueKind == JsonValueKind.Object)
            .SelectMany(static operation => operation.Value.TryGetProperty("parameters", out JsonElement parameters)
                ? parameters.EnumerateArray()
                : [])];
        Assert.DoesNotContain(allParameters, static parameter =>
            parameter.TryGetProperty("in", out JsonElement location) &&
            string.Equals(location.GetString(), "query", StringComparison.Ordinal) &&
            parameter.TryGetProperty("name", out JsonElement name) &&
            name.GetString() is "userId" or "actorUserId");

        JsonElement productCreate = paths.GetProperty("/api/v{version}/products").GetProperty("post");
        JsonElement idempotencyParameter = Assert.Single(productCreate.GetProperty("parameters").EnumerateArray(), static parameter =>
            string.Equals(parameter.GetProperty("name").GetString(), "Idempotency-Key", StringComparison.Ordinal));
        JsonElement schema = idempotencyParameter.GetProperty("schema");

        Assert.Multiple(
            () => Assert.Equal("header", idempotencyParameter.GetProperty("in").GetString()),
            () => Assert.False(idempotencyParameter.TryGetProperty("required", out JsonElement optionalRequired) &&
                optionalRequired.GetBoolean()),
            () => Assert.Equal(1, schema.GetProperty("minLength").GetInt32()),
            () => Assert.Equal(128, schema.GetProperty("maxLength").GetInt32()),
            () => Assert.True(productCreate.GetProperty("responses").TryGetProperty("400", out _)),
            () => Assert.True(productCreate.GetProperty("responses").TryGetProperty("409", out _)));

        JsonElement aiCreate = paths.GetProperty("/api/v{version}/ai/food/vision").GetProperty("post");
        JsonElement requiredIdempotency = Assert.Single(aiCreate.GetProperty("parameters").EnumerateArray(), static parameter =>
            string.Equals(parameter.GetProperty("name").GetString(), "Idempotency-Key", StringComparison.Ordinal));
        Assert.True(requiredIdempotency.GetProperty("required").GetBoolean());

        JsonElement refresh = paths.GetProperty("/api/v{version}/auth/refresh").GetProperty("post");
        Assert.False(refresh.TryGetProperty("parameters", out JsonElement refreshParameters) &&
            refreshParameters.EnumerateArray().Any(static parameter =>
                string.Equals(parameter.GetProperty("name").GetString(), "Idempotency-Key", StringComparison.Ordinal)));

        JsonElement dashboardTestEmail = paths.GetProperty("/api/v{version}/dashboard/test-email").GetProperty("post");
        JsonElement scheduledTestNotification = paths.GetProperty("/api/v{version}/notifications/test/schedule").GetProperty("post");
        Assert.Multiple(
            () => Assert.True(dashboardTestEmail.GetProperty("responses").TryGetProperty("429", out _)),
            () => Assert.True(scheduledTestNotification.GetProperty("responses").TryGetProperty("429", out _)));
    }

    [Fact]
    public void Kestrel_DefaultRequestBodyLimit_IsOneMegabyte() {
        KestrelServerOptions options = apiFactory.Services.GetRequiredService<IOptions<KestrelServerOptions>>().Value;

        Assert.Equal(1024 * 1024, options.Limits.MaxRequestBodySize);
    }

    [Fact]
    public async Task SwaggerJson_MatchesFocusedPresentationContractSnapshot() {
        HttpClient client = apiFactory.CreateClient();
        HttpResponseMessage response = await client.GetAsync("/swagger/v1/swagger.json");
        response.EnsureSuccessStatusCode();

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        string actual = BuildFocusedOpenApiSnapshot(json.RootElement);
        await AssertSnapshotAsync("openapi-focused-contract.json", actual);
    }

    [Fact]
    public async Task SwaggerJson_MatchesAuthAdminContractSnapshot() {
        HttpClient client = apiFactory.CreateClient();
        HttpResponseMessage response = await client.GetAsync("/swagger/v1/swagger.json");
        response.EnsureSuccessStatusCode();

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        string actual = BuildAuthAdminOpenApiSnapshot(json.RootElement);
        await AssertSnapshotAsync("openapi-auth-admin-contract.json", actual);
    }

    [Fact]
    public async Task SwaggerJson_MatchesFullPresentationContractSnapshot() {
        HttpClient client = apiFactory.CreateClient();
        HttpResponseMessage response = await client.GetAsync("/swagger/v1/swagger.json");
        response.EnsureSuccessStatusCode();

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        string actual = BuildFullOpenApiSnapshot(json.RootElement);
        await AssertSnapshotAsync("openapi-full-contract.json", actual);
    }

    [Fact]
    public async Task EmailVerificationHub_Negotiate_RequiresAuthentication() {
        HttpClient client = apiFactory.CreateClient();

        HttpResponseMessage response = await client.PostAsync("/hubs/email-verification/negotiate?negotiateVersion=1", content: null);
        ErrorPayload? payload = await response.Content.ReadFromJsonAsync<ErrorPayload>(JsonOptions);

        Assert.NotNull(payload);
        Assert.Multiple(
            () => Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode),
            () => Assert.Contains(response.Headers.WwwAuthenticate, static header =>
                string.Equals(header.Scheme, "Bearer", StringComparison.Ordinal)),
            () => Assert.Equal("Authentication.Unauthorized", payload.Error),
            () => Assert.Equal("Authentication is required.", payload.Message),
            () => Assert.False(string.IsNullOrWhiteSpace(payload.TraceId)));
    }

    [Fact]
    public async Task EmailVerificationHub_Negotiate_WithAccessTokenQuery_ReturnsConnectionInfo() {
        HttpClient client = apiFactory.CreateClient();
        string accessToken = await RegisterAndGetAccessTokenAsync(client);

        HttpResponseMessage response = await client.PostAsync($"/hubs/email-verification/negotiate?negotiateVersion=1&access_token={accessToken}", content: null);
        NegotiatePayload? payload = await response.Content.ReadFromJsonAsync<NegotiatePayload>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload.ConnectionId));
        Assert.False(string.IsNullOrWhiteSpace(payload.ConnectionToken));
    }

    [Fact]
    public async Task UnhandledException_ReturnsStandardErrorContractWithTraceId() {
        HttpClient client = apiFactory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/test/exceptions/unhandled");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        JsonElement root = json.RootElement;

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("Server.Unexpected", root.GetProperty("error").GetString());
        Assert.Equal("An unexpected error occurred.", root.GetProperty("message").GetString());
        Assert.True(root.TryGetProperty("traceId", out JsonElement traceIdProperty));
        Assert.False(string.IsNullOrWhiteSpace(traceIdProperty.GetString()));
    }

    private static async Task<string> RegisterAndGetAccessTokenAsync(HttpClient client) {
        string email = $"api-tests-{Guid.NewGuid():N}@example.com";
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterHttpRequest(email, "Password123!", "en")).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        AuthPayload? payload = await response.Content.ReadFromJsonAsync<AuthPayload>(JsonOptions).ConfigureAwait(false);
        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload.AccessToken));
        return payload.AccessToken;
    }

    private static HttpClient CreateTestUserClient(WebApplicationFactory<Program> factory, Guid userId) {
        HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.AuthenticateHeader, "true");
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, userId.ToString("D"));
        return client;
    }

    private static string BuildFocusedOpenApiSnapshot(JsonElement root) {
        string[] selectedPaths = [
            "/api/v{version}/auth/register",
            "/api/v{version}/auth/login",
            "/api/v{version}/products",
            "/api/v{version}/products/{id}",
            "/api/v{version}/recipes",
            "/api/v{version}/recipes/{id}",
            "/api/v{version}/statistics",
            "/api/v{version}/users/info",
            "/api/v{version}/users/preferences/appearance",
            "/api/v{version}/users/overview",
            "/api/v{version}/weight-entries",
            "/api/v{version}/waist-entries",
        ];

        JsonElement paths = root.GetProperty("paths");
        EndpointSnapshot[] endpoints = [.. selectedPaths
            .Select(path => CreateEndpointSnapshot(paths, path))
            .OrderBy(endpoint => endpoint.Path, StringComparer.Ordinal)];

        var snapshot = new OpenApiFocusedSnapshot(
            root.GetProperty("openapi").GetString() ?? string.Empty,
            endpoints);

        return JsonSerializer.Serialize(snapshot, IndentedJsonOptions);
    }

    private static string BuildAuthAdminOpenApiSnapshot(JsonElement root) {
        string[] selectedPaths = [
            "/api/v{version}/auth/register",
            "/api/v{version}/auth/login",
            "/api/v{version}/auth/google",
            "/api/v{version}/auth/refresh",
            "/api/v{version}/auth/restore",
            "/api/v{version}/auth/verify-email",
            "/api/v{version}/auth/verify-email/resend",
            "/api/v{version}/auth/password-reset/request",
            "/api/v{version}/auth/password-reset/confirm",
            "/api/v{version}/auth/admin-sso/start",
            "/api/v{version}/auth/admin-sso/exchange",
            "/api/v{version}/users/password",
            "/api/v{version}/users/password/set",
            "/api/v{version}/admin/dashboard",
            "/api/v{version}/admin/users",
            "/api/v{version}/admin/users/impersonation-sessions",
            "/api/v{version}/admin/users/login-events",
            "/api/v{version}/admin/users/login-summary",
            "/api/v{version}/admin/users/{id}",
            "/api/v{version}/admin/users/{id}/impersonation",
            "/api/v{version}/admin/users/{id}/password",
            "/api/v{version}/admin/billing/subscriptions",
            "/api/v{version}/admin/billing/payments",
            "/api/v{version}/admin/billing/webhook-events",
            "/api/v{version}/admin/email-templates",
            "/api/v{version}/admin/email-templates/test",
            "/api/v{version}/admin/email-templates/{key}/{locale}",
            "/api/v{version}/admin/ai-usage/summary",
        ];

        JsonElement paths = root.GetProperty("paths");
        EndpointSnapshot[] endpoints = [.. selectedPaths
            .Select(path => CreateEndpointSnapshot(paths, path))
            .OrderBy(endpoint => endpoint.Path, StringComparer.Ordinal)];

        var snapshot = new OpenApiFocusedSnapshot(
            root.GetProperty("openapi").GetString() ?? string.Empty,
            endpoints);

        return JsonSerializer.Serialize(snapshot, IndentedJsonOptions);
    }

    private static string BuildFullOpenApiSnapshot(JsonElement root) {
        JsonElement paths = root.GetProperty("paths");
        EndpointSnapshot[] endpoints = [.. paths.EnumerateObject()
            .Select(property => CreateEndpointSnapshot(paths, property.Name))
            .OrderBy(endpoint => endpoint.Path, StringComparer.Ordinal)];

        var snapshot = new OpenApiFocusedSnapshot(
            root.GetProperty("openapi").GetString() ?? string.Empty,
            endpoints,
            CreateSchemaSnapshots(root));

        return JsonSerializer.Serialize(snapshot, IndentedJsonOptions);
    }

    private static EndpointSnapshot CreateEndpointSnapshot(JsonElement paths, string path) {
        JsonElement pathNode = paths.GetProperty(path);
        OperationSnapshot[] operations = [.. pathNode.EnumerateObject()
            .Select(operation => new OperationSnapshot(
                operation.Name,
                operation.Value.TryGetProperty("requestBody", out _),
                operation.Value.TryGetProperty("responses", out JsonElement responses)
                    ? [.. responses.EnumerateObject()
                        .Select(response => response.Name)
                        .Order(StringComparer.Ordinal)]
                    : Array.Empty<string>(),
                CreateParameterSnapshots(operation.Value)))
            .OrderBy(operation => operation.Method, StringComparer.Ordinal)];

        return new EndpointSnapshot(path.ToLowerInvariant(), operations);
    }

    private static IReadOnlyList<OpenApiParameterSnapshot>? CreateParameterSnapshots(JsonElement operation) {
        if (!operation.TryGetProperty("parameters", out JsonElement parameters) || parameters.ValueKind != JsonValueKind.Array) {
            return null;
        }

        OpenApiParameterSnapshot[] snapshots = [.. parameters.EnumerateArray()
            .Where(parameter => parameter.TryGetProperty("in", out JsonElement location) &&
                string.Equals(location.GetString(), "query", StringComparison.Ordinal))
            .Select(parameter => {
                JsonElement schema = parameter.GetProperty("schema");
                return new OpenApiParameterSnapshot(
                    parameter.GetProperty("name").GetString() ?? string.Empty,
                    parameter.GetProperty("in").GetString() ?? string.Empty,
                    parameter.TryGetProperty("required", out JsonElement required) && required.GetBoolean(),
                    schema.TryGetProperty("type", out JsonElement type) ? type.GetString() ?? string.Empty : string.Empty,
                    schema.TryGetProperty("format", out JsonElement format) ? format.GetString() : null,
                    schema.TryGetProperty("default", out JsonElement defaultValue) ? defaultValue.GetRawText() : null);
            })
            .OrderBy(parameter => parameter.Location, StringComparer.Ordinal)
            .ThenBy(parameter => parameter.Name, StringComparer.Ordinal)];

        return snapshots.Length == 0 ? null : snapshots;
    }

    private static IReadOnlyList<OpenApiSchemaSnapshot>? CreateSchemaSnapshots(JsonElement root) {
        if (!root.TryGetProperty("components", out JsonElement components) ||
            !components.TryGetProperty("schemas", out JsonElement schemas) ||
            schemas.ValueKind != JsonValueKind.Object) {
            return null;
        }

        OpenApiSchemaSnapshot[] snapshots = [.. schemas.EnumerateObject()
            .Select(schema => {
                HashSet<string> required = schema.Value.TryGetProperty("required", out JsonElement requiredNode)
                    ? [.. requiredNode.EnumerateArray().Select(item => item.GetString() ?? string.Empty)]
                    : [];
                OpenApiSchemaPropertySnapshot[] properties = schema.Value.TryGetProperty(
                    "properties",
                    out JsonElement propertyNodes)
                    ? [.. propertyNodes.EnumerateObject()
                        .Select(property => CreateSchemaPropertySnapshot(
                            property.Name,
                            property.Value,
                            required.Contains(property.Name)))
                        .OrderBy(property => property.Name, StringComparer.Ordinal)]
                    : [];
                return new OpenApiSchemaSnapshot(schema.Name, properties);
            })
            .OrderBy(schema => schema.Name, StringComparer.Ordinal)];

        return snapshots.Length == 0 ? null : snapshots;
    }

    private static OpenApiSchemaPropertySnapshot CreateSchemaPropertySnapshot(
        string name,
        JsonElement property,
        bool required) {
        JsonElement items = property.TryGetProperty("items", out JsonElement itemsNode)
            ? itemsNode
            : default;
        return new OpenApiSchemaPropertySnapshot(
            name,
            required,
            property.TryGetProperty("type", out JsonElement type) ? type.GetString() : null,
            property.TryGetProperty("format", out JsonElement format) ? format.GetString() : null,
            property.TryGetProperty("$ref", out JsonElement reference) ? reference.GetString() : null,
            property.TryGetProperty("nullable", out JsonElement nullable) && nullable.GetBoolean(),
            items.ValueKind == JsonValueKind.Object && items.TryGetProperty("type", out JsonElement itemType)
                ? itemType.GetString()
                : null,
            items.ValueKind == JsonValueKind.Object && items.TryGetProperty("$ref", out JsonElement itemReference)
                ? itemReference.GetString()
                : null);
    }

    private static async Task AssertErrorContractSnapshotAsync(string scenario, ErrorPayload payload) {
        string snapshotPath = SnapshotPathResolver.GetPath("error-contract-snapshots.json");
        JsonObject snapshotRoot = JsonNode.Parse(await File.ReadAllTextAsync(snapshotPath).ConfigureAwait(false))!.AsObject();
        string? expected = snapshotRoot[scenario]?.ToJsonString(ErrorSnapshotJsonOptions);
        string actual = JsonSerializer.Serialize(
            new ErrorContractSnapshot(payload.Error, payload.Message, payload.Errors),
            ErrorSnapshotJsonOptions);

        Assert.NotNull(expected);
        Assert.Equal(
            expected.ReplaceLineEndings("\n").TrimEnd(),
            actual.ReplaceLineEndings("\n").TrimEnd());
    }

    private static async Task AssertSnapshotAsync(string snapshotFileName, string actual) {
        string snapshotPath = SnapshotPathResolver.GetPath(snapshotFileName);
        if (string.Equals(Environment.GetEnvironmentVariable("UPDATE_CONTRACT_SNAPSHOTS"), "1", StringComparison.Ordinal)) {
            await File.WriteAllTextAsync(snapshotPath, actual.ReplaceLineEndings("\n")).ConfigureAwait(false);
        }

        string expected = await File.ReadAllTextAsync(snapshotPath).ConfigureAwait(false);
        Assert.Equal(
            expected.ReplaceLineEndings("\n").TrimEnd(),
            actual.ReplaceLineEndings("\n").TrimEnd());
    }

    [ExcludeFromCodeCoverage]
    private sealed record AuthPayload(string AccessToken);

    [ExcludeFromCodeCoverage]
    private sealed record ErrorPayload(string Error, string Message, string? TraceId = null, IReadOnlyDictionary<string, string[]>? Errors = null);

    [ExcludeFromCodeCoverage]
    private sealed record ErrorContractSnapshot(string Error, string Message, IReadOnlyDictionary<string, string[]>? Errors = null);

    [ExcludeFromCodeCoverage]
    private sealed record NegotiatePayload(string ConnectionId, string ConnectionToken);

    [ExcludeFromCodeCoverage]
    private sealed record ApiVersionPayload(
        string CommitSha,
        string ImageTag,
        string Environment,
        string ApplicationVersion,
        DateTimeOffset StartedAtUtc);

    [ExcludeFromCodeCoverage]
    private sealed record OpenApiFocusedSnapshot(
        string OpenApi,
        IReadOnlyList<EndpointSnapshot> Endpoints,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<OpenApiSchemaSnapshot>? Schemas = null);

    [ExcludeFromCodeCoverage]
    private sealed record EndpointSnapshot(string Path, IReadOnlyList<OperationSnapshot> Operations);

    [ExcludeFromCodeCoverage]
    private sealed record OperationSnapshot(
        string Method,
        bool HasRequestBody,
        IReadOnlyList<string> ResponseCodes,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<OpenApiParameterSnapshot>? QueryParameters);

    [ExcludeFromCodeCoverage]
    private sealed record OpenApiParameterSnapshot(
        string Name,
        string Location,
        bool Required,
        string Type,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Format,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Default);

    [ExcludeFromCodeCoverage]
    private sealed record OpenApiSchemaSnapshot(
        string Name,
        IReadOnlyList<OpenApiSchemaPropertySnapshot> Properties);

    [ExcludeFromCodeCoverage]
    private sealed record OpenApiSchemaPropertySnapshot(
        string Name,
        bool Required,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Type,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Format,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Reference,
        bool Nullable,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ItemType,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? ItemReference);
}
