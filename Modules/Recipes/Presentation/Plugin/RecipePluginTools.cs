using System.ComponentModel;
using System.Text.Json;
using FoodDiary.Mediator;
using FoodDiary.Modules.Recipes.Application.Models;
using FoodDiary.Modules.Recipes.Presentation.Mappings;
using FoodDiary.Modules.Recipes.Presentation.Requests;
using FoodDiary.Presentation.Api.Policies;
using FoodDiary.Results;
using Microsoft.AspNetCore.Http;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace FoodDiary.Modules.Recipes.Presentation.Plugin;

[McpServerToolType]
public sealed class RecipePluginTools(ISender sender, IHttpContextAccessor http, RecipePluginLinks links) {
    [McpServerTool(Name = RecipePluginAccess.RecipeTool, Title = "Save private recipe", ReadOnly = false, Destructive = false, OpenWorld = false)]
    [McpMeta("securitySchemes", JsonValue = "[{\"type\":\"oauth2\",\"scopes\":[\"" + RecipePluginAccess.RecipeScope + "\"]}]")]
    [Description("Save a private recipe to the connected FoodDiary account after the user asks to save it.")]
    public async Task<CallToolResult> SavePrivateRecipeAsync(
        string name, int servings, IReadOnlyList<PluginRecipeStep> steps, CancellationToken cancellationToken) {
        if (string.IsNullOrWhiteSpace(name) || servings <= 0 ||
            steps is not { Count: > 0 } || steps.Count > 30 ||
            steps.Any(static step => step?.Ingredients is null ||
                step.Ingredients.Any(static ingredient => ingredient is null || string.IsNullOrWhiteSpace(ingredient.Name)))) {
            return Error("Provide a recipe name, servings, and cooking steps with valid ingredient names.");
        }

        var webRequest = new CreateRecipeHttpRequest(
            name, Description: null, Comment: null, Category: null, ImageUrl: null, ImageAssetId: null,
            PrepTime: null, CookTime: null, servings, "Private", CalculateNutritionAutomatically: true,
            ManualCalories: null, ManualProteins: null, ManualFats: null, ManualCarbs: null,
            ManualFiber: null, ManualAlcohol: null, steps.Select(ToHttpStep).ToArray());
        Guid owner = RecipePluginAccess.GetOwner(http.HttpContext!.User);
        Result<RecipeModel> result = await sender.Send(webRequest.ToCommand(owner), cancellationToken);
        if (result.IsFailure) {
            return Error("FoodDiary could not save this recipe. Check the name, portions, steps, and ingredient details.");
        }

        RecipeModel recipe = result.Value;
        JsonElement saved = JsonSerializer.SerializeToElement(new {
            id = recipe.Id,
            name = recipe.Name,
            visibility = "Private",
            editUrl = links.Recipe(recipe.Id).AbsoluteUri,
        });
        return new CallToolResult {
            Content = [new TextContentBlock { Text = saved.GetRawText() }],
            StructuredContent = saved,
        };
    }

    private static RecipeStepHttpRequest ToHttpStep(PluginRecipeStep step) => new(
        Title: null, step.Description, step.Ingredients.Select(static ingredient =>
            new RecipeIngredientHttpRequest(ProductId: null, NestedRecipeId: null, Amount: 0) {
                TextName = ingredient.Name,
                AmountText = ingredient.AmountText,
            }).ToArray(), ImageUrl: null, ImageAssetId: null);

    private static CallToolResult Error(string text) => new() {
        IsError = true,
        Content = [new TextContentBlock { Text = text }],
    };

    public sealed record PluginRecipeStep(string Description, IReadOnlyList<PluginRecipeIngredient> Ingredients);
    public sealed record PluginRecipeIngredient(string Name, string? AmountText = null);
}
