using System.Text.Json;
using System.Text.Json.Nodes;
using FoodDiary.Modules.Products.Application.Models;
using FoodDiary.Modules.Products.Application.Commands.ImportCatalogProduct;
using FoodDiary.Modules.Products.Presentation.Mappings;
using FoodDiary.Modules.Products.Presentation.Requests;

namespace FoodDiary.Modules.Products.Presentation.Tests;

[ExcludeFromCodeCoverage]
public sealed class CatalogProductHttpMappingsTests {
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ExportedCatalogJson_CanBeImportedWithoutLosingFields(bool preview) {
        var product = new CatalogProductModel(Guid.NewGuid(), "Овсянка", "12345678", "Brand", "Solid",
            "cereals", "Whole grain", "https://example.test/product.png", "G", 100, 45, 370, 12, 7, 60, 9, 0.5);
        var userId = Guid.NewGuid();
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        JsonNode? legacyJson = JsonSerializer.SerializeToNode(product, options);
        JsonNode? exportJson = JsonSerializer.SerializeToNode(product.ToHttpResponse(), options);
        CatalogProductHttpRequest request = Assert.IsType<CatalogProductHttpRequest>(
            exportJson!.Deserialize<CatalogProductHttpRequest>(options));

        ImportCatalogProductCommand command = request.ToImportCommand(userId, preview);

        Assert.Multiple(
            () => Assert.True(JsonNode.DeepEquals(legacyJson, exportJson), "Catalog export JSON changed."),
            () => Assert.Equal(product, command.Product),
            () => Assert.Equal(userId, command.UserId),
            () => Assert.Equal(preview, command.Preview));
    }
}
