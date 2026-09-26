using FoodDiary.Modules.Recipes.Presentation.Requests;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace FoodDiary.Web.Api.IntegrationTests;

public sealed partial class RecipePostgresApiFlowTests {
    [RequiresDockerFact]
    public async Task PublicRecipes_AnonymousCatalogAndDetail_RespectVisibilityAndPrivateIngredients() {
        HttpClient owner = factory.CreateClient();
        owner.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await RegisterAndGetAccessTokenAsync(owner));
        HttpClient guest = factory.CreateClient();
        Guid productId = (await CreateIngredientProductAsync(owner)).Id;
        string name = $"Public catalog {Guid.NewGuid():N}";
        Guid recipeId = await CreateRecipeAsync(owner, productId, name);
        RecipeStepHttpRequest[] steps = [new("Cook", "Cook recipe", [new RecipeIngredientHttpRequest(productId, NestedRecipeId: null, 200)], ImageUrl: null, ImageAssetId: null)];

        Assert.Equal(HttpStatusCode.NotFound, (await guest.GetAsync($"/api/v1/recipes/public/{recipeId}")).StatusCode);
        (await owner.PatchAsJsonAsync($"/api/v1/recipes/{recipeId}", new { visibility = "Public", calculateNutritionAutomatically = true, steps })).EnsureSuccessStatusCode();
        HttpResponseMessage detail = await guest.GetAsync($"/api/v1/recipes/public/{recipeId}");
        detail.EnsureSuccessStatusCode();
        using var body = JsonDocument.Parse(await detail.Content.ReadAsStringAsync());
        JsonElement root = body.RootElement;
        JsonElement ingredient = root.GetProperty("steps")[0].GetProperty("ingredients")[0];
        Assert.Multiple(
            () => Assert.Equal(name, root.GetProperty("name").GetString()),
            () => Assert.False(root.TryGetProperty("comment", out _)),
            () => Assert.False(root.TryGetProperty("userId", out _)),
            () => Assert.False(root.TryGetProperty("isFavorite", out _)),
            () => Assert.True(detail.Headers.CacheControl?.NoStore),
            () => Assert.False(ingredient.GetProperty("isAvailable").GetBoolean()),
            () => Assert.Equal(JsonValueKind.Null, ingredient.GetProperty("name").ValueKind));

        using var catalog = JsonDocument.Parse(await guest.GetStringAsync($"/api/v1/recipes/public?search={Uri.EscapeDataString(name)}&limit=20"));
        Assert.Equal(recipeId, Assert.Single(catalog.RootElement.GetProperty("data").EnumerateArray()).GetProperty("id").GetGuid());
        using var filtered = JsonDocument.Parse(await guest.GetStringAsync($"/api/v1/recipes/public?search={Uri.EscapeDataString(name)}&maxTotalTime=15"));
        Assert.Equal(0, filtered.RootElement.GetProperty("totalItems").GetInt32());
        Assert.Equal(HttpStatusCode.BadRequest, (await guest.GetAsync("/api/v1/recipes/public?limit=51")).StatusCode);

        (await owner.PatchAsJsonAsync($"/api/v1/recipes/{recipeId}", new { visibility = "Private", calculateNutritionAutomatically = true, steps })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.NotFound, (await guest.GetAsync($"/api/v1/recipes/public/{recipeId}")).StatusCode);
        using var hidden = JsonDocument.Parse(await guest.GetStringAsync($"/api/v1/recipes/public?search={Uri.EscapeDataString(name)}"));
        Assert.Equal(0, hidden.RootElement.GetProperty("totalItems").GetInt32());
    }
}
