using System.Text.Json;
using System.Text.Json.Nodes;
using FoodDiary.Modules.Recipes.Application.Common;
using FoodDiary.Modules.Recipes.Application.Commands.ImportCatalogRecipe;
using FoodDiary.Modules.Recipes.Application.Models;
using FoodDiary.Modules.Recipes.Presentation.Mappings;
using FoodDiary.Modules.Recipes.Presentation.Requests;

namespace FoodDiary.Modules.Recipes.Presentation.Tests;

[ExcludeFromCodeCoverage]
public sealed class CatalogRecipeHttpMappingsTests {
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ExportedCatalogJson_PreservesStepOrderAndAllIngredientFieldsOnImport(bool preview) {
        var productId = Guid.NewGuid();
        var nestedRecipeId = Guid.NewGuid();
        var imageAssetId = Guid.NewGuid();
        var recipe = new CatalogRecipeModel(Guid.NewGuid(), "Суп", "Description", "soups",
            "https://example.test/recipe.png", 10, 20, 3, "ru", LanguageConfirmed: true, CalculateNutritionAutomatically: false, 100, 2, 3, 4, 5, 6, [
                new RecipeStepInput(7, "Mix", "Title", "https://example.test/step.png", imageAssetId, [
                    new RecipeIngredientInput(productId, NestedRecipeId: null, 25) {
                        PublicName = "Carrot", PublicUnit = "G", TextName = "морковь", AmountText = "one",
                    },
                    new RecipeIngredientInput(ProductId: null, nestedRecipeId, 50),
                ]) { ImageAssetIds = [imageAssetId] },
            ]);
        var userId = Guid.NewGuid();
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        JsonNode? legacyJson = JsonSerializer.SerializeToNode(recipe, options);
        JsonNode? exportJson = JsonSerializer.SerializeToNode(recipe.ToHttpResponse(), options);
        CatalogRecipeHttpRequest request = Assert.IsType<CatalogRecipeHttpRequest>(
            exportJson!.Deserialize<CatalogRecipeHttpRequest>(options));

        ImportCatalogRecipeCommand command = request.ToImportCommand(userId, preview);

        Assert.Multiple(
            () => Assert.True(JsonNode.DeepEquals(legacyJson, exportJson), "Catalog export JSON changed."),
            () => Assert.True(JsonNode.DeepEquals(legacyJson, JsonSerializer.SerializeToNode(command.Recipe, options)),
                "Import mapping changed catalog fields."),
            () => Assert.Equal(userId, command.UserId),
            () => Assert.Equal(preview, command.Preview),
            () => Assert.Equal(7, Assert.Single(command.Recipe.Steps).Order));
    }

    [Theory]
    [InlineData("null")]
    [InlineData("[null]")]
    [InlineData("[{\"order\":1,\"description\":\"Mix\",\"ingredients\":null}]")]
    [InlineData("[{\"order\":1,\"description\":\"Mix\",\"ingredients\":[null]}]")]
    public void MalformedNestedLists_ReachApplicationValidationWithoutThrowing(string stepsJson) {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        string json = "{\"id\":\"11111111-1111-1111-1111-111111111111\",\"name\":\"Recipe\",\"steps\":" + stepsJson + "}";
        CatalogRecipeHttpRequest request = Assert.IsType<CatalogRecipeHttpRequest>(
            JsonSerializer.Deserialize<CatalogRecipeHttpRequest>(json, options));

        ImportCatalogRecipeCommand command = request.ToImportCommand(Guid.NewGuid(), preview: true);

        JsonNode? actualSteps = JsonSerializer.SerializeToNode(command.Recipe, options)!["steps"];
        Assert.True(JsonNode.DeepEquals(JsonSerializer.SerializeToNode(request, options)!["steps"], actualSteps),
            "Malformed list shapes must be preserved for the existing catalog validation.");
    }
}
