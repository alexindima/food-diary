using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Presentation.Api.Features.Auth.Requests;
using FoodDiary.Presentation.Api.Features.Images.Requests;
using FoodDiary.Presentation.Api.Features.Products.Requests;
using FoodDiary.Presentation.Api.Features.Recipes.Requests;
using FoodDiary.Presentation.Api.Features.ShoppingLists.Requests;
using FoodDiary.Presentation.Api.Features.WeightEntries.Requests;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Web.Api.IntegrationTests;

public sealed class PostgresCriticalApiFlowTests(PostgresApiWebApplicationFactory factory)
    : IClassFixture<PostgresApiWebApplicationFactory> {
    private static readonly JsonSerializerOptions JsonOptions = new() {
        PropertyNameCaseInsensitive = true,
    };

    [RequiresDockerFact]
    public async Task Register_ThenAccessProtectedEndpoint_WorksAgainstPostgres() {
        var client = factory.CreateClient();
        var email = $"postgres-api-tests-{Guid.NewGuid():N}@example.com";

        var registerResponse = await client.PostAsJsonAsync(
            "/api/v1/v1/auth/register",
            new RegisterHttpRequest(email, "Password123!", "en"));
        var authPayload = await registerResponse.Content.ReadFromJsonAsync<AuthPayload>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);
        Assert.NotNull(authPayload);
        Assert.False(string.IsNullOrWhiteSpace(authPayload.AccessToken));
        Assert.Equal(email, authPayload.User.Email);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authPayload.AccessToken);
        var usersInfoResponse = await client.GetAsync("/api/v1/v1/users/info");

        Assert.Equal(HttpStatusCode.OK, usersInfoResponse.StatusCode);
    }

    [RequiresDockerFact]
    public async Task Refresh_WithIssuedRefreshToken_ReturnsNewAccessTokenAgainstPostgres() {
        var client = factory.CreateClient();
        var email = $"postgres-refresh-tests-{Guid.NewGuid():N}@example.com";

        var registerResponse = await client.PostAsJsonAsync(
            "/api/v1/v1/auth/register",
            new RegisterHttpRequest(email, "Password123!", "en"));
        var registerPayload = await registerResponse.Content.ReadFromJsonAsync<AuthPayload>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);
        Assert.NotNull(registerPayload);
        Assert.False(string.IsNullOrWhiteSpace(registerPayload.RefreshToken));

        var originalRefreshToken = registerPayload.RefreshToken;
        var refreshResponse = await client.PostAsJsonAsync(
            "/api/v1/v1/auth/refresh",
            new RefreshTokenHttpRequest(originalRefreshToken));
        var refreshPayload = await refreshResponse.Content.ReadFromJsonAsync<AuthPayload>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
        Assert.NotNull(refreshPayload);
        Assert.False(string.IsNullOrWhiteSpace(refreshPayload.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(refreshPayload.RefreshToken));
        Assert.NotEqual(originalRefreshToken, refreshPayload.RefreshToken);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", refreshPayload.AccessToken);
        var usersInfoResponse = await client.GetAsync("/api/v1/v1/users/info");

        Assert.Equal(HttpStatusCode.OK, usersInfoResponse.StatusCode);

        var replayResponse = await client.PostAsJsonAsync(
            "/api/v1/v1/auth/refresh",
            new RefreshTokenHttpRequest(originalRefreshToken));

        Assert.Equal(HttpStatusCode.Unauthorized, replayResponse.StatusCode);
    }

    [RequiresDockerFact]
    public async Task DeleteUser_ThenRestoreAccount_ReturnsFreshTokensAgainstPostgres() {
        var client = factory.CreateClient();
        var email = $"postgres-restore-tests-{Guid.NewGuid():N}@example.com";
        const string password = "Password123!";

        var registerResponse = await client.PostAsJsonAsync(
            "/api/v1/v1/auth/register",
            new RegisterHttpRequest(email, password, "en"));
        var registerPayload = await registerResponse.Content.ReadFromJsonAsync<AuthPayload>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);
        Assert.NotNull(registerPayload);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", registerPayload.AccessToken);

        var deleteResponse = await client.DeleteAsync("/api/v1/v1/users");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        client.DefaultRequestHeaders.Authorization = null;

        var restoreResponse = await client.PostAsJsonAsync(
            "/api/v1/v1/auth/restore",
            new RestoreAccountHttpRequest(email, password));
        var restorePayload = await restoreResponse.Content.ReadFromJsonAsync<AuthPayload>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, restoreResponse.StatusCode);
        Assert.NotNull(restorePayload);
        Assert.False(string.IsNullOrWhiteSpace(restorePayload.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(restorePayload.RefreshToken));

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", restorePayload.AccessToken);
        var usersInfoResponse = await client.GetAsync("/api/v1/v1/users/info");

        Assert.Equal(HttpStatusCode.OK, usersInfoResponse.StatusCode);
    }

    [RequiresDockerFact]
    public async Task RequestPasswordReset_PersistsResetTokenAgainstPostgres() {
        var client = factory.CreateClient();
        var email = $"postgres-password-reset-tests-{Guid.NewGuid():N}@example.com";

        var registerResponse = await client.PostAsJsonAsync(
            "/api/v1/v1/auth/register",
            new RegisterHttpRequest(email, "Password123!", "en"));

        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        var passwordResetResponse = await client.PostAsJsonAsync(
            "/api/v1/v1/auth/password-reset/request",
            new RequestPasswordResetHttpRequest(email));

        Assert.Equal(HttpStatusCode.NoContent, passwordResetResponse.StatusCode);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FoodDiaryDbContext>();
        var user = dbContext.Users.Single(u => u.Email == email);

        Assert.False(string.IsNullOrWhiteSpace(user.PasswordResetTokenHash));
        Assert.NotNull(user.PasswordResetTokenExpiresAtUtc);
        Assert.NotNull(user.PasswordResetSentAtUtc);
    }

    [RequiresDockerFact]
    public async Task ConfirmPasswordReset_ReturnsFreshAuthenticationAgainstPostgres() {
        var client = factory.CreateClient();
        var email = $"postgres-password-reset-confirm-tests-{Guid.NewGuid():N}@example.com";
        const string oldPassword = "Password123!";
        const string newPassword = "Password456!";

        factory.EmailSender.Clear();

        var registerResponse = await client.PostAsJsonAsync(
            "/api/v1/v1/auth/register",
            new RegisterHttpRequest(email, oldPassword, "en"));

        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        var passwordResetRequestResponse = await client.PostAsJsonAsync(
            "/api/v1/v1/auth/password-reset/request",
            new RequestPasswordResetHttpRequest(email));

        Assert.Equal(HttpStatusCode.NoContent, passwordResetRequestResponse.StatusCode);

        var resetMessage = factory.EmailSender.GetRequiredPasswordResetMessage(email);
        var confirmResponse = await client.PostAsJsonAsync(
            "/api/v1/v1/auth/password-reset/confirm",
            new ConfirmPasswordResetHttpRequest(Guid.Parse(resetMessage.UserId), resetMessage.Token, newPassword));
        var confirmPayload = await confirmResponse.Content.ReadFromJsonAsync<AuthPayload>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);
        Assert.NotNull(confirmPayload);
        Assert.False(string.IsNullOrWhiteSpace(confirmPayload.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(confirmPayload.RefreshToken));
        Assert.Equal(email, confirmPayload.User.Email);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", confirmPayload.AccessToken);
        var usersInfoResponse = await client.GetAsync("/api/v1/v1/users/info");
        Assert.Equal(HttpStatusCode.OK, usersInfoResponse.StatusCode);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FoodDiaryDbContext>();
        var user = dbContext.Users.Single(u => u.Email == email);

        Assert.Null(user.PasswordResetTokenHash);
        Assert.Null(user.PasswordResetTokenExpiresAtUtc);
        Assert.NotNull(user.LastLoginAtUtc);
    }

    [RequiresDockerFact]
    public async Task CreateWeightEntry_WithDuplicateDate_ReturnsConflictAgainstPostgres() {
        var client = factory.CreateClient();
        var accessToken = await RegisterAndGetAccessTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var request = new CreateWeightEntryHttpRequest(
            new DateTime(2026, 3, 27, 18, 45, 0, DateTimeKind.Unspecified),
            80.5);

        var firstResponse = await client.PostAsJsonAsync("/api/v1/v1/weight-entries", request);
        var duplicateResponse = await client.PostAsJsonAsync("/api/v1/v1/weight-entries", request);
        var payload = await duplicateResponse.Content.ReadFromJsonAsync<ErrorPayload>(JsonOptions);

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("WeightEntry.AlreadyExists", payload.Error);
    }

    [RequiresDockerFact]
    public async Task DeleteProduct_KeepsShoppingListItemButClearsProductReferenceAgainstPostgres() {
        var client = factory.CreateClient();
        var accessToken = await RegisterAndGetAccessTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var createProductResponse = await client.PostAsJsonAsync(
            "/api/v1/v1/products",
            new CreateProductHttpRequest(
                null,
                "Relational Product",
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
        var product = await createProductResponse.Content.ReadFromJsonAsync<ProductPayload>(JsonOptions);

        await AssertStatusCodeAsync(HttpStatusCode.Created, createProductResponse);
        Assert.NotNull(product);

        var createShoppingListResponse = await client.PostAsJsonAsync(
            "/api/v1/v1/shopping-lists",
            new CreateShoppingListHttpRequest(
                "Postgres relational list",
                [
                    new ShoppingListItemHttpRequest(
                        product.Id,
                        "Relational Product",
                        2,
                        "pcs",
                        "Test",
                        false,
                        0)
                ]));

        await AssertStatusCodeAsync(HttpStatusCode.Created, createShoppingListResponse);

        var deleteProductResponse = await client.DeleteAsync($"/api/v1/v1/products/{product.Id}");
        var currentShoppingListResponse = await client.GetAsync("/api/v1/v1/shopping-lists/current");
        var currentShoppingList = await currentShoppingListResponse.Content.ReadFromJsonAsync<ShoppingListPayload>(JsonOptions);

        await AssertStatusCodeAsync(HttpStatusCode.NoContent, deleteProductResponse);
        await AssertStatusCodeAsync(HttpStatusCode.OK, currentShoppingListResponse);
        Assert.NotNull(currentShoppingList);

        var item = Assert.Single(currentShoppingList.Items);
        Assert.Null(item.ProductId);
        Assert.Equal("Relational Product", item.Name);
    }

    [RequiresDockerFact]
    public async Task DeleteImageAsset_BlocksReferencedRecipeAssets_ThenReturnsNotFoundAfterRecipeDeletionAgainstPostgres() {
        var client = factory.CreateClient();
        var accessToken = await RegisterAndGetAccessTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var createProductResponse = await client.PostAsJsonAsync(
            "/api/v1/v1/products",
            new CreateProductHttpRequest(
                null,
                "Recipe Asset Product",
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
        var product = await createProductResponse.Content.ReadFromJsonAsync<ProductPayload>(JsonOptions);

        await AssertStatusCodeAsync(HttpStatusCode.Created, createProductResponse);
        Assert.NotNull(product);

        var recipeAsset = await CreateImageAssetAsync(client, "recipe-photo.jpg");
        var stepAsset = await CreateImageAssetAsync(client, "step-photo.jpg");

        var createRecipeResponse = await client.PostAsJsonAsync(
            "/api/v1/v1/recipes",
            new CreateRecipeHttpRequest(
                "Recipe With Assets",
                "Relational image usage",
                null,
                "Dinner",
                recipeAsset.FileUrl,
                recipeAsset.AssetId,
                10,
                20,
                2,
                "private",
                true,
                null,
                null,
                null,
                null,
                null,
                null,
                [
                    new RecipeStepHttpRequest(
                        "Step 1",
                        "Use the uploaded image.",
                        [
                            new RecipeIngredientHttpRequest(product.Id, null, 1)
                        ],
                        stepAsset.FileUrl,
                        stepAsset.AssetId)
                ]));
        var recipe = await createRecipeResponse.Content.ReadFromJsonAsync<RecipePayload>(JsonOptions);

        await AssertStatusCodeAsync(HttpStatusCode.Created, createRecipeResponse);
        Assert.NotNull(recipe);

        var deleteRecipeAssetWhileInUse = await client.DeleteAsync($"/api/v1/v1/images/{recipeAsset.AssetId}");
        var deleteStepAssetWhileInUse = await client.DeleteAsync($"/api/v1/v1/images/{stepAsset.AssetId}");
        var recipeAssetError = await deleteRecipeAssetWhileInUse.Content.ReadFromJsonAsync<ErrorPayload>(JsonOptions);
        var stepAssetError = await deleteStepAssetWhileInUse.Content.ReadFromJsonAsync<ErrorPayload>(JsonOptions);

        await AssertStatusCodeAsync(HttpStatusCode.Conflict, deleteRecipeAssetWhileInUse);
        await AssertStatusCodeAsync(HttpStatusCode.Conflict, deleteStepAssetWhileInUse);
        Assert.NotNull(recipeAssetError);
        Assert.NotNull(stepAssetError);
        Assert.Equal("Image.InUse", recipeAssetError.Error);
        Assert.Equal("Image.InUse", stepAssetError.Error);

        var deleteRecipeResponse = await client.DeleteAsync($"/api/v1/v1/recipes/{recipe.Id}");
        var deleteRecipeAssetAfterRecipeDeletion = await client.DeleteAsync($"/api/v1/v1/images/{recipeAsset.AssetId}");
        var deleteStepAssetAfterRecipeDeletion = await client.DeleteAsync($"/api/v1/v1/images/{stepAsset.AssetId}");
        var deletedRecipeAssetError = await deleteRecipeAssetAfterRecipeDeletion.Content.ReadFromJsonAsync<ErrorPayload>(JsonOptions);
        var deletedStepAssetError = await deleteStepAssetAfterRecipeDeletion.Content.ReadFromJsonAsync<ErrorPayload>(JsonOptions);

        await AssertStatusCodeAsync(HttpStatusCode.NoContent, deleteRecipeResponse);
        await AssertStatusCodeAsync(HttpStatusCode.NotFound, deleteRecipeAssetAfterRecipeDeletion);
        await AssertStatusCodeAsync(HttpStatusCode.NotFound, deleteStepAssetAfterRecipeDeletion);
        Assert.NotNull(deletedRecipeAssetError);
        Assert.NotNull(deletedStepAssetError);
        Assert.Equal("Image.NotFound", deletedRecipeAssetError.Error);
        Assert.Equal("Image.NotFound", deletedStepAssetError.Error);
    }

    private static async Task AssertStatusCodeAsync(HttpStatusCode expected, HttpResponseMessage response) {
        if (response.StatusCode == expected) {
            return;
        }

        var body = await response.Content.ReadAsStringAsync();
        Assert.Fail(
            $"Expected status {(int)expected} ({expected}), got {(int)response.StatusCode} ({response.StatusCode}). Body: {body}");
    }

    private static async Task<string> RegisterAndGetAccessTokenAsync(HttpClient client) {
        var email = $"postgres-api-tests-{Guid.NewGuid():N}@example.com";
        var response = await client.PostAsJsonAsync(
            "/api/v1/v1/auth/register",
            new RegisterHttpRequest(email, "Password123!", "en"));

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<AuthPayload>(JsonOptions);
        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload.AccessToken));
        return payload.AccessToken;
    }

    private static async Task<ImageUploadPayload> CreateImageAssetAsync(HttpClient client, string fileName) {
        var response = await client.PostAsJsonAsync(
            "/api/v1/v1/images/upload-url",
            new GetImageUploadUrlHttpRequest(fileName, "image/jpeg", 4096));

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<ImageUploadPayload>(JsonOptions);
        Assert.NotNull(payload);
        Assert.NotEqual(Guid.Empty, payload.AssetId);
        return payload;
    }

    private sealed record AuthPayload(string AccessToken, string RefreshToken, AuthUserPayload User);

    private sealed record AuthUserPayload(string Email);

    private sealed record ProductPayload(Guid Id);

    private sealed record RecipePayload(Guid Id);

    private sealed record ImageUploadPayload(string UploadUrl, string FileUrl, string ObjectKey, DateTime ExpiresAtUtc, Guid AssetId);

    private sealed record ShoppingListPayload(Guid Id, string Name, IReadOnlyList<ShoppingListItemPayload> Items);

    private sealed record ShoppingListItemPayload(Guid Id, Guid ShoppingListId, Guid? ProductId, string Name);

    private sealed record ErrorPayload(string Error, string Message, string? TraceId = null, IReadOnlyDictionary<string, string[]>? Errors = null);
}
