using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Domain.Entities.Users;
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

[ExcludeFromCodeCoverage]
public sealed class PostgresCriticalApiFlowTests(PostgresApiWebApplicationFactory factory)
    : IClassFixture<PostgresApiWebApplicationFactory> {
    private static readonly JsonSerializerOptions JsonOptions = new() {
        PropertyNameCaseInsensitive = true,
    };

    [RequiresDockerFact]
    public async Task Register_ThenAccessProtectedEndpoint_WorksAgainstPostgres() {
        HttpClient client = factory.CreateClient();
        string email = $"postgres-api-tests-{Guid.NewGuid():N}@example.com";

        HttpResponseMessage registerResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterHttpRequest(email, "Password123!", "en"));
        AuthPayload? authPayload = await registerResponse.Content.ReadFromJsonAsync<AuthPayload>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);
        Assert.NotNull(authPayload);
        Assert.False(string.IsNullOrWhiteSpace(authPayload.AccessToken));
        Assert.Equal(email, authPayload.User.Email);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authPayload.AccessToken);
        HttpResponseMessage usersInfoResponse = await client.GetAsync("/api/v1/users/info");

        Assert.Equal(HttpStatusCode.OK, usersInfoResponse.StatusCode);
    }

    [RequiresDockerFact]
    public async Task Refresh_WithIssuedRefreshToken_ReturnsNewAccessTokenAgainstPostgres() {
        HttpClient client = factory.CreateClient();
        string email = $"postgres-refresh-tests-{Guid.NewGuid():N}@example.com";

        HttpResponseMessage registerResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterHttpRequest(email, "Password123!", "en"));
        AuthPayload? registerPayload = await registerResponse.Content.ReadFromJsonAsync<AuthPayload>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);
        Assert.NotNull(registerPayload);
        Assert.False(string.IsNullOrWhiteSpace(registerPayload.RefreshToken));

        string originalRefreshToken = registerPayload.RefreshToken;
        HttpResponseMessage refreshResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new RefreshTokenHttpRequest(originalRefreshToken));
        AuthPayload? refreshPayload = await refreshResponse.Content.ReadFromJsonAsync<AuthPayload>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
        Assert.NotNull(refreshPayload);
        Assert.False(string.IsNullOrWhiteSpace(refreshPayload.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(refreshPayload.RefreshToken));
        Assert.NotEqual(originalRefreshToken, refreshPayload.RefreshToken);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", refreshPayload.AccessToken);
        HttpResponseMessage usersInfoResponse = await client.GetAsync("/api/v1/users/info");

        Assert.Equal(HttpStatusCode.OK, usersInfoResponse.StatusCode);

        HttpResponseMessage replayResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/refresh",
            new RefreshTokenHttpRequest(originalRefreshToken));

        Assert.Equal(HttpStatusCode.Unauthorized, replayResponse.StatusCode);
    }

    [RequiresDockerFact]
    public async Task DeleteUser_ThenRestoreAccount_ReturnsFreshTokensAgainstPostgres() {
        HttpClient client = factory.CreateClient();
        string email = $"postgres-restore-tests-{Guid.NewGuid():N}@example.com";
        const string password = "Password123!";

        HttpResponseMessage registerResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterHttpRequest(email, password, "en"));
        AuthPayload? registerPayload = await registerResponse.Content.ReadFromJsonAsync<AuthPayload>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);
        Assert.NotNull(registerPayload);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", registerPayload.AccessToken);

        HttpResponseMessage deleteResponse = await client.DeleteAsync("/api/v1/users");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        client.DefaultRequestHeaders.Authorization = null;

        HttpResponseMessage restoreResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/restore",
            new RestoreAccountHttpRequest(email, password));
        AuthPayload? restorePayload = await restoreResponse.Content.ReadFromJsonAsync<AuthPayload>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, restoreResponse.StatusCode);
        Assert.NotNull(restorePayload);
        Assert.False(string.IsNullOrWhiteSpace(restorePayload.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(restorePayload.RefreshToken));

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", restorePayload.AccessToken);
        HttpResponseMessage usersInfoResponse = await client.GetAsync("/api/v1/users/info");

        Assert.Equal(HttpStatusCode.OK, usersInfoResponse.StatusCode);
    }

    [RequiresDockerFact]
    public async Task DeleteUser_ThenRestoreAccount_InvalidatesOutstandingPasswordResetToken() {
        HttpClient client = factory.CreateClient();
        string email = $"postgres-restore-reset-tests-{Guid.NewGuid():N}@example.com";
        const string password = "Password123!";

        factory.EmailSender.Clear();

        HttpResponseMessage registerResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterHttpRequest(email, password, "en"));
        AuthPayload? registerPayload = await registerResponse.Content.ReadFromJsonAsync<AuthPayload>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);
        Assert.NotNull(registerPayload);

        HttpResponseMessage passwordResetRequestResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/password-reset/request",
            new RequestPasswordResetHttpRequest(email));

        Assert.Equal(HttpStatusCode.NoContent, passwordResetRequestResponse.StatusCode);

        PasswordResetMessage resetMessage = factory.EmailSender.GetRequiredPasswordResetMessage(email);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", registerPayload.AccessToken);
        HttpResponseMessage deleteResponse = await client.DeleteAsync("/api/v1/users");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        using (IServiceScope scope = factory.Services.CreateScope()) {
            FoodDiaryDbContext dbContext = scope.ServiceProvider.GetRequiredService<FoodDiaryDbContext>();
            User deletedUser = dbContext.Users.Single(u => u.Email == email);

            Assert.Null(deletedUser.PasswordResetTokenHash);
            Assert.Null(deletedUser.PasswordResetTokenExpiresAtUtc);
            Assert.Null(deletedUser.PasswordResetSentAtUtc);
        }

        client.DefaultRequestHeaders.Authorization = null;

        HttpResponseMessage restoreResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/restore",
            new RestoreAccountHttpRequest(email, password));

        Assert.Equal(HttpStatusCode.OK, restoreResponse.StatusCode);

        HttpResponseMessage confirmResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/password-reset/confirm",
            new ConfirmPasswordResetHttpRequest(Guid.Parse(resetMessage.UserId), resetMessage.Token, "Password456!"));

        Assert.Equal(HttpStatusCode.Unauthorized, confirmResponse.StatusCode);
    }

    [RequiresDockerFact]
    public async Task RequestPasswordReset_PersistsResetTokenAgainstPostgres() {
        HttpClient client = factory.CreateClient();
        string email = $"postgres-password-reset-tests-{Guid.NewGuid():N}@example.com";

        HttpResponseMessage registerResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterHttpRequest(email, "Password123!", "en"));

        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        HttpResponseMessage passwordResetResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/password-reset/request",
            new RequestPasswordResetHttpRequest(email));

        Assert.Equal(HttpStatusCode.NoContent, passwordResetResponse.StatusCode);

        using IServiceScope scope = factory.Services.CreateScope();
        FoodDiaryDbContext dbContext = scope.ServiceProvider.GetRequiredService<FoodDiaryDbContext>();
        User user = dbContext.Users.Single(u => u.Email == email);

        Assert.False(string.IsNullOrWhiteSpace(user.PasswordResetTokenHash));
        Assert.NotNull(user.PasswordResetTokenExpiresAtUtc);
        Assert.NotNull(user.PasswordResetSentAtUtc);
    }

    [RequiresDockerFact]
    public async Task ConfirmPasswordReset_ReturnsFreshAuthenticationAgainstPostgres() {
        HttpClient client = factory.CreateClient();
        string email = $"postgres-password-reset-confirm-tests-{Guid.NewGuid():N}@example.com";
        const string oldPassword = "Password123!";
        const string newPassword = "Password456!";

        factory.EmailSender.Clear();

        HttpResponseMessage registerResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterHttpRequest(email, oldPassword, "en"));

        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        HttpResponseMessage passwordResetRequestResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/password-reset/request",
            new RequestPasswordResetHttpRequest(email));

        Assert.Equal(HttpStatusCode.NoContent, passwordResetRequestResponse.StatusCode);

        PasswordResetMessage resetMessage = factory.EmailSender.GetRequiredPasswordResetMessage(email);
        HttpResponseMessage confirmResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/password-reset/confirm",
            new ConfirmPasswordResetHttpRequest(Guid.Parse(resetMessage.UserId), resetMessage.Token, newPassword));
        AuthPayload? confirmPayload = await confirmResponse.Content.ReadFromJsonAsync<AuthPayload>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);
        Assert.NotNull(confirmPayload);
        Assert.False(string.IsNullOrWhiteSpace(confirmPayload.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(confirmPayload.RefreshToken));
        Assert.Equal(email, confirmPayload.User.Email);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", confirmPayload.AccessToken);
        HttpResponseMessage usersInfoResponse = await client.GetAsync("/api/v1/users/info");
        Assert.Equal(HttpStatusCode.OK, usersInfoResponse.StatusCode);

        using IServiceScope scope = factory.Services.CreateScope();
        FoodDiaryDbContext dbContext = scope.ServiceProvider.GetRequiredService<FoodDiaryDbContext>();
        User user = dbContext.Users.Single(u => u.Email == email);

        Assert.Null(user.PasswordResetTokenHash);
        Assert.Null(user.PasswordResetTokenExpiresAtUtc);
        Assert.NotNull(user.LastLoginAtUtc);
    }

    [RequiresDockerFact]
    public async Task CreateWeightEntry_WithDuplicateDate_ReturnsConflictAgainstPostgres() {
        HttpClient client = factory.CreateClient();
        string accessToken = await RegisterAndGetAccessTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var request = new CreateWeightEntryHttpRequest(
            new DateTime(2026, 3, 27, 18, 45, 0, DateTimeKind.Unspecified),
            80.5);

        HttpResponseMessage firstResponse = await client.PostAsJsonAsync("/api/v1/weight-entries", request);
        HttpResponseMessage duplicateResponse = await client.PostAsJsonAsync("/api/v1/weight-entries", request);
        ErrorPayload? payload = await duplicateResponse.Content.ReadFromJsonAsync<ErrorPayload>(JsonOptions);

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("WeightEntry.AlreadyExists", payload.Error);
    }

    [RequiresDockerFact]
    public async Task DeleteProduct_KeepsShoppingListItemButClearsProductReferenceAgainstPostgres() {
        HttpClient client = factory.CreateClient();
        string accessToken = await RegisterAndGetAccessTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        HttpResponseMessage createProductResponse = await client.PostAsJsonAsync(
            "/api/v1/products",
            new CreateProductHttpRequest(
                Barcode: null,
                "Relational Product",
                Brand: null,
                "Unknown",
                Category: null,
                Description: null,
                Comment: null,
                ImageUrl: null,
                ImageAssetId: null,
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
        ProductPayload? product = await createProductResponse.Content.ReadFromJsonAsync<ProductPayload>(JsonOptions);

        await AssertStatusCodeAsync(HttpStatusCode.Created, createProductResponse);
        Assert.NotNull(product);

        HttpResponseMessage createShoppingListResponse = await client.PostAsJsonAsync(
            "/api/v1/shopping-lists",
            new CreateShoppingListHttpRequest(
                "Postgres relational list",
                [
                    new ShoppingListItemHttpRequest(
                        product.Id,
                        "Relational Product",
                        2,
                        "pcs",
                        "Test",
                        IsChecked: false,
                        0),
                ]));

        await AssertStatusCodeAsync(HttpStatusCode.Created, createShoppingListResponse);

        HttpResponseMessage deleteProductResponse = await client.DeleteAsync($"/api/v1/products/{product.Id}");
        HttpResponseMessage currentShoppingListResponse = await client.GetAsync("/api/v1/shopping-lists/current");
        ShoppingListPayload? currentShoppingList = await currentShoppingListResponse.Content.ReadFromJsonAsync<ShoppingListPayload>(JsonOptions);

        await AssertStatusCodeAsync(HttpStatusCode.NoContent, deleteProductResponse);
        await AssertStatusCodeAsync(HttpStatusCode.OK, currentShoppingListResponse);
        Assert.NotNull(currentShoppingList);

        ShoppingListItemPayload item = Assert.Single(currentShoppingList.Items);
        Assert.Null(item.ProductId);
        Assert.Equal("Relational Product", item.Name);
    }

    [RequiresDockerFact]
    public async Task DeleteImageAsset_BlocksReferencedRecipeAssets_ThenReturnsNotFoundAfterRecipeDeletionAgainstPostgres() {
        HttpClient client = factory.CreateClient();
        string accessToken = await RegisterAndGetAccessTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        HttpResponseMessage createProductResponse = await client.PostAsJsonAsync(
            "/api/v1/products",
            new CreateProductHttpRequest(
                Barcode: null,
                "Recipe Asset Product",
                Brand: null,
                "Unknown",
                Category: null,
                Description: null,
                Comment: null,
                ImageUrl: null,
                ImageAssetId: null,
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
        ProductPayload? product = await createProductResponse.Content.ReadFromJsonAsync<ProductPayload>(JsonOptions);

        await AssertStatusCodeAsync(HttpStatusCode.Created, createProductResponse);
        Assert.NotNull(product);

        ImageUploadPayload recipeAsset = await CreateImageAssetAsync(client, "recipe-photo.jpg");
        ImageUploadPayload stepAsset = await CreateImageAssetAsync(client, "step-photo.jpg");

        HttpResponseMessage createRecipeResponse = await client.PostAsJsonAsync(
            "/api/v1/recipes",
            new CreateRecipeHttpRequest(
                "Recipe With Assets",
                "Relational image usage",
                Comment: null,
                "Dinner",
                recipeAsset.FileUrl,
                recipeAsset.AssetId,
                10,
                20,
                2,
                "private",
                CalculateNutritionAutomatically: true,
                ManualCalories: null,
                ManualProteins: null,
                ManualFats: null,
                ManualCarbs: null,
                ManualFiber: null,
                ManualAlcohol: null,
                [
                    new RecipeStepHttpRequest(
                        "Step 1",
                        "Use the uploaded image.",
                        [
                            new RecipeIngredientHttpRequest(product.Id, NestedRecipeId: null, 1),
                        ],
                        stepAsset.FileUrl,
                        stepAsset.AssetId),
                ]));
        RecipePayload? recipe = await createRecipeResponse.Content.ReadFromJsonAsync<RecipePayload>(JsonOptions);

        await AssertStatusCodeAsync(HttpStatusCode.Created, createRecipeResponse);
        Assert.NotNull(recipe);

        HttpResponseMessage deleteRecipeAssetWhileInUse = await client.DeleteAsync($"/api/v1/images/{recipeAsset.AssetId}");
        HttpResponseMessage deleteStepAssetWhileInUse = await client.DeleteAsync($"/api/v1/images/{stepAsset.AssetId}");
        ErrorPayload? recipeAssetError = await deleteRecipeAssetWhileInUse.Content.ReadFromJsonAsync<ErrorPayload>(JsonOptions);
        ErrorPayload? stepAssetError = await deleteStepAssetWhileInUse.Content.ReadFromJsonAsync<ErrorPayload>(JsonOptions);

        await AssertStatusCodeAsync(HttpStatusCode.Conflict, deleteRecipeAssetWhileInUse);
        await AssertStatusCodeAsync(HttpStatusCode.Conflict, deleteStepAssetWhileInUse);
        Assert.NotNull(recipeAssetError);
        Assert.NotNull(stepAssetError);
        Assert.Equal("Image.InUse", recipeAssetError.Error);
        Assert.Equal("Image.InUse", stepAssetError.Error);

        HttpResponseMessage deleteRecipeResponse = await client.DeleteAsync($"/api/v1/recipes/{recipe.Id}");
        HttpResponseMessage deleteRecipeAssetAfterRecipeDeletion = await client.DeleteAsync($"/api/v1/images/{recipeAsset.AssetId}");
        HttpResponseMessage deleteStepAssetAfterRecipeDeletion = await client.DeleteAsync($"/api/v1/images/{stepAsset.AssetId}");
        ErrorPayload? deletedRecipeAssetError = await deleteRecipeAssetAfterRecipeDeletion.Content.ReadFromJsonAsync<ErrorPayload>(JsonOptions);
        ErrorPayload? deletedStepAssetError = await deleteStepAssetAfterRecipeDeletion.Content.ReadFromJsonAsync<ErrorPayload>(JsonOptions);

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

        string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        Assert.Fail(
            string.Create(CultureInfo.InvariantCulture, $"Expected status {(int)expected} ({expected}), got {(int)response.StatusCode} ({response.StatusCode}). Body: {body}"));
    }

    private static async Task<string> RegisterAndGetAccessTokenAsync(HttpClient client) {
        string email = $"postgres-api-tests-{Guid.NewGuid():N}@example.com";
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterHttpRequest(email, "Password123!", "en")).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        AuthPayload? payload = await response.Content.ReadFromJsonAsync<AuthPayload>(JsonOptions).ConfigureAwait(false);
        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload.AccessToken));
        return payload.AccessToken;
    }

    private static async Task<ImageUploadPayload> CreateImageAssetAsync(HttpClient client, string fileName) {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/images/upload-url",
            new GetImageUploadUrlHttpRequest(fileName, "image/jpeg", 4096)).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        ImageUploadPayload? payload = await response.Content.ReadFromJsonAsync<ImageUploadPayload>(JsonOptions).ConfigureAwait(false);
        Assert.NotNull(payload);
        Assert.NotEqual(Guid.Empty, payload.AssetId);
        return payload;
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
    private sealed record ImageUploadPayload(string UploadUrl, string FileUrl, DateTime ExpiresAtUtc, Guid AssetId);

    [ExcludeFromCodeCoverage]
    private sealed record ShoppingListPayload(Guid Id, string Name, IReadOnlyList<ShoppingListItemPayload> Items);

    [ExcludeFromCodeCoverage]
    private sealed record ShoppingListItemPayload(Guid Id, Guid ShoppingListId, Guid? ProductId, string Name);

    [ExcludeFromCodeCoverage]
    private sealed record ErrorPayload(string Error, string Message, string? TraceId = null, IReadOnlyDictionary<string, string[]>? Errors = null);
}
