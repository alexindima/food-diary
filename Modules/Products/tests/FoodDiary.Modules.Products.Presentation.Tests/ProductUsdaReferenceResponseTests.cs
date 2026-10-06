using System.Text.Json;
using FoodDiary.Modules.Products.Application.Models;
using FoodDiary.Modules.Products.Presentation.Mappings;
using FoodDiary.Modules.Products.Presentation.Responses;

namespace FoodDiary.Modules.Products.Presentation.Tests;

[ExcludeFromCodeCoverage]
public sealed class ProductUsdaReferenceResponseTests {
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    [Theory]
    [InlineData(null)]
    [InlineData(171077)]
    [InlineData(2667078)]
    public void ProductResponse_PreservesNullableLocalAndProviderFdcIdentifiers(int? fdcId) {
        var model = new ProductModel(Guid.NewGuid(), Barcode: null, "Reference fixture", Brand: null,
            "Unknown", Category: null, Description: null, Comment: null, ImageUrl: null, ImageAssetId: null,
            "G", 100, 250, 203, 1.4, 0, 47.3, 0, 0, 0, "Private", DateTime.UtcNow,
            IsOwnedByCurrentUser: true, 80, "green", fdcId, IsFavorite: false, FavoriteProductId: null);

        ProductHttpResponse response = model.ToHttpResponse();
        using var wire = JsonDocument.Parse(JsonSerializer.Serialize(response, JsonOptions));
        JsonElement reference = wire.RootElement.GetProperty("usdaFdcId");

        Assert.Equal(fdcId, response.UsdaFdcId);
        if (fdcId.HasValue) {
            Assert.Equal(fdcId.Value, reference.GetInt32());
        } else {
            Assert.Equal(JsonValueKind.Null, reference.ValueKind);
        }
    }
}
