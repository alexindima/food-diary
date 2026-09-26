using FoodDiary.Modules.Identity.Contracts.Authentication.Common;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Identity.Presentation.Features.Auth.Requests;
using FoodDiary.Modules.Images.Presentation.Requests;
using FoodDiary.Modules.Products.Presentation.Requests;
using FoodDiary.Modules.Recipes.Presentation.Requests;
using FoodDiary.Modules.MealPlanning.Presentation.ShoppingLists.Requests;
using FoodDiary.Modules.BodyMetrics.Presentation.Features.WeightEntries.Requests;
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
        Assert.NotEqual(originalRefreshToken, refreshPayload.RefreshToken, StringComparer.Ordinal);

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

        await using (AsyncServiceScope scope = factory.Services.CreateAsyncScope()) {
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

        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();
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

        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();
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
                        Id: null,
                        ProductId: product.Id,
                        Name: "Relational Product",
                        Amount: 2,
                        Unit: "pcs",
                        Category: "Test",
                        Aisle: null,
                        Note: null,
                        IsChecked: false,
                        CheckedOnUtc: null,
                        SortOrder: 0),
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
    public async Task DeleteImageAsset_BlocksReferencedRecipeAssets_ThenDeletesAfterRecipeDeletionAgainstPostgres() {
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
        ImageUploadPayload galleryAsset = await CreateImageAssetAsync(client, "gallery-photo.jpg");
        ImageUploadPayload stepGalleryAsset = await CreateImageAssetAsync(client, "step-detail.jpg");

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
                        stepAsset.AssetId) { ImageAssetIds = [stepAsset.AssetId, stepGalleryAsset.AssetId] },
                ]) { ImageAssetIds = [recipeAsset.AssetId, galleryAsset.AssetId] });
        RecipePayload? recipe = await createRecipeResponse.Content.ReadFromJsonAsync<RecipePayload>(JsonOptions);

        await AssertStatusCodeAsync(HttpStatusCode.Created, createRecipeResponse);
        Assert.NotNull(recipe);

        using var savedRecipe = JsonDocument.Parse(await client.GetStringAsync($"/api/v1/recipes/{recipe.Id}"));
        JsonElement savedStepImages = savedRecipe.RootElement.GetProperty("steps")[0].GetProperty("images");
        Assert.Equal(2, savedStepImages.GetArrayLength());
        Assert.Equal(stepGalleryAsset.AssetId, savedStepImages[1].GetProperty("imageAssetId").GetGuid());
        await AssertStatusCodeAsync(HttpStatusCode.Conflict, await client.DeleteAsync($"/api/v1/images/{stepGalleryAsset.AssetId}"));
        JsonElement savedImages = savedRecipe.RootElement.GetProperty("images");
        Assert.Equal(2, savedImages.GetArrayLength());
        Assert.Equal(recipeAsset.AssetId, savedImages[0].GetProperty("imageAssetId").GetGuid());
        Assert.Equal(galleryAsset.AssetId, savedImages[1].GetProperty("imageAssetId").GetGuid());
        await AssertStatusCodeAsync(HttpStatusCode.Conflict, await client.DeleteAsync($"/api/v1/images/{galleryAsset.AssetId}"));

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
        HttpResponseMessage deleteRecipeAssetAgain = await client.DeleteAsync($"/api/v1/images/{recipeAsset.AssetId}");
        HttpResponseMessage deleteStepAssetAgain = await client.DeleteAsync($"/api/v1/images/{stepAsset.AssetId}");

        await AssertStatusCodeAsync(HttpStatusCode.NoContent, deleteRecipeResponse);
        await AssertStatusCodeAsync(HttpStatusCode.NoContent, deleteRecipeAssetAfterRecipeDeletion);
        await AssertStatusCodeAsync(HttpStatusCode.NoContent, deleteStepAssetAfterRecipeDeletion);
        await AssertStatusCodeAsync(HttpStatusCode.NotFound, deleteRecipeAssetAgain);
        await AssertStatusCodeAsync(HttpStatusCode.NotFound, deleteStepAssetAgain);

        ErrorPayload? deletedRecipeAssetError = await deleteRecipeAssetAgain.Content.ReadFromJsonAsync<ErrorPayload>(JsonOptions);
        ErrorPayload? deletedStepAssetError = await deleteStepAssetAgain.Content.ReadFromJsonAsync<ErrorPayload>(JsonOptions);

        Assert.NotNull(deletedRecipeAssetError);
        Assert.NotNull(deletedStepAssetError);
        Assert.Equal("Image.NotFound", deletedRecipeAssetError.Error);
        Assert.Equal("Image.NotFound", deletedStepAssetError.Error);
    }

    [RequiresDockerFact]
    public async Task RecipeTextIngredients_RoundTripDuplicateAndNutritionAgainstPostgres() {
        HttpClient client = factory.CreateClient();
        string accessToken = await RegisterAndGetAccessTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var steps = new[] { new { description = "Season", ingredients = new[] { new { textName = "Salt", amountText = "to taste", amount = 0 }, new { textName = "Tomatoes", amountText = "2 pieces", amount = 0 } } } };
        HttpResponseMessage created = await client.PostAsJsonAsync("/api/v1/recipes", new {
            name = "Text recipe", servings = 2, visibility = "Private", calculateNutritionAutomatically = true, steps,
        });
        await AssertStatusCodeAsync(HttpStatusCode.Created, created);
        using var body = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        Guid id = body.RootElement.GetProperty("id").GetGuid();
        using var saved = JsonDocument.Parse(await client.GetStringAsync($"/api/v1/recipes/{id}"));
        Assert.Equal(new[] { "Salt", "Tomatoes" }, saved.RootElement.GetProperty("steps")[0].GetProperty("ingredients").EnumerateArray().Select(item => item.GetProperty("textName").GetString()), StringComparer.Ordinal);
        JsonElement ingredient = saved.RootElement.GetProperty("steps")[0].GetProperty("ingredients")[0];
        Assert.Multiple(
            () => Assert.Equal("Salt", ingredient.GetProperty("textName").GetString()),
            () => Assert.Equal("to taste", ingredient.GetProperty("amountText").GetString()),
            () => Assert.Equal(2, saved.RootElement.GetProperty("missingIngredientCount").GetInt32()),
            () => Assert.Equal(JsonValueKind.Null, saved.RootElement.GetProperty("totalCalories").ValueKind));

        HttpResponseMessage duplicate = await client.PostAsJsonAsync($"/api/v1/recipes/{id}/duplicate", new { });
        await AssertStatusCodeAsync(HttpStatusCode.OK, duplicate);
        using var copy = JsonDocument.Parse(await duplicate.Content.ReadAsStringAsync());
        Assert.Equal("Salt", copy.RootElement.GetProperty("steps")[0].GetProperty("ingredients")[0].GetProperty("textName").GetString());

        HttpResponseMessage nested = await client.PostAsJsonAsync("/api/v1/recipes", new {
            name = "Nested text recipe", servings = 1, visibility = "Private", calculateNutritionAutomatically = true,
            steps = new[] { new { description = "Mix", ingredients = new[] { new { nestedRecipeId = id, amount = 1 } } } },
        });
        await AssertStatusCodeAsync(HttpStatusCode.Created, nested);
        using var nestedBody = JsonDocument.Parse(await nested.Content.ReadAsStringAsync());
        Assert.Equal(2, nestedBody.RootElement.GetProperty("missingIngredientCount").GetInt32());
        Assert.Equal(JsonValueKind.Null, nestedBody.RootElement.GetProperty("totalCalories").ValueKind);

        Guid copyId = copy.RootElement.GetProperty("id").GetGuid();
        HttpResponseMessage updated = await client.PatchAsJsonAsync($"/api/v1/recipes/{copyId}", new {
            calculateNutritionAutomatically = false, manualCalories = 100, manualProteins = 5, manualFats = 4, manualCarbs = 10,
            manualFiber = 0, manualAlcohol = 0, steps,
        });
        await AssertStatusCodeAsync(HttpStatusCode.OK, updated);
        using var reloaded = JsonDocument.Parse(await client.GetStringAsync($"/api/v1/recipes/{copyId}"));
        Assert.Equal(new[] { "Salt", "Tomatoes" }, reloaded.RootElement.GetProperty("steps")[0].GetProperty("ingredients").EnumerateArray().Select(item => item.GetProperty("textName").GetString()), StringComparer.Ordinal);
        using var manual = JsonDocument.Parse(await updated.Content.ReadAsStringAsync());
        Assert.Equal(0, manual.RootElement.GetProperty("missingIngredientCount").GetInt32());
        Assert.Equal(100, manual.RootElement.GetProperty("totalCalories").GetDouble());

        HttpResponseMessage partial = await client.PostAsJsonAsync("/api/v1/recipes", new {
            name = "Partial recipe", servings = 1, visibility = "Private", calculateNutritionAutomatically = true,
            steps = new[] { new { description = "Mix", ingredients = new RecipeIngredientHttpRequest[] {
                new(ProductId: null, NestedRecipeId: copyId, Amount: 1),
                new(ProductId: null, NestedRecipeId: null, Amount: 0) { TextName = "Pepper" },
            }, }, },
        });
        await AssertStatusCodeAsync(HttpStatusCode.Created, partial);
        using var partialBody = JsonDocument.Parse(await partial.Content.ReadAsStringAsync());
        Assert.Equal(1, partialBody.RootElement.GetProperty("missingIngredientCount").GetInt32());
        Assert.Equal(50, partialBody.RootElement.GetProperty("totalCalories").GetDouble());
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
        HttpResponseMessage confirmResponse = await client.PostAsJsonAsync(
            $"/api/v1/images/{payload.AssetId}/confirm",
            new { }).ConfigureAwait(false);
        confirmResponse.EnsureSuccessStatusCode();
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
