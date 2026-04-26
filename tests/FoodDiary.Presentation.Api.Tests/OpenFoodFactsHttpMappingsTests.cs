using FoodDiary.Application.Abstractions.OpenFoodFacts.Models;
using FoodDiary.Presentation.Api.Features.OpenFoodFacts.Mappings;

namespace FoodDiary.Presentation.Api.Tests;

public sealed class OpenFoodFactsHttpMappingsTests {
    [Fact]
    public void ToQuery_MapsBarcode() {
        var query = OpenFoodFactsHttpMappings.ToQuery("4600000000001");

        Assert.Equal("4600000000001", query.Barcode);
    }

    [Fact]
    public void ToHttpResponse_WhenModelIsNull_ReturnsNull() {
        OpenFoodFactsProductModel? model = null;

        var response = model.ToHttpResponse();

        Assert.Null(response);
    }

    [Fact]
    public void ToHttpResponse_MapsAllFields() {
        var model = new OpenFoodFactsProductModel(
            "4600000000001",
            "Молоко 3.2%",
            "Простоквашино",
            "Dairy",
            "https://images.openfoodfacts.org/test.jpg",
            60,
            3.2,
            3.2,
            4.7,
            0);

        var response = model.ToHttpResponse();

        Assert.NotNull(response);
        Assert.Equal("4600000000001", response.Barcode);
        Assert.Equal("Молоко 3.2%", response.Name);
        Assert.Equal("Простоквашино", response.Brand);
        Assert.Equal("Dairy", response.Category);
        Assert.Equal("https://images.openfoodfacts.org/test.jpg", response.ImageUrl);
        Assert.Equal(60, response.CaloriesPer100G);
        Assert.Equal(3.2, response.ProteinsPer100G);
        Assert.Equal(3.2, response.FatsPer100G);
        Assert.Equal(4.7, response.CarbsPer100G);
        Assert.Equal(0, response.FiberPer100G);
    }

    [Fact]
    public void ToSearchQuery_MapsSearchAndLimit() {
        var query = OpenFoodFactsHttpMappings.ToSearchQuery("milk", 15);

        Assert.Equal("milk", query.Search);
        Assert.Equal(15, query.Limit);
    }

    [Fact]
    public void ToListHttpResponse_MapsAllItems() {
        var models = new List<OpenFoodFactsProductModel> {
            new("111", "Product A", "Brand A", null, null, 100, 5, 3, 15, 1),
            new("222", "Product B", null, "Cat B", null, null, null, null, null, null),
        };

        var responses = models.ToListHttpResponse();

        Assert.Equal(2, responses.Count);
        Assert.Equal("111", responses[0].Barcode);
        Assert.Equal("Product A", responses[0].Name);
        Assert.Equal("222", responses[1].Barcode);
        Assert.Null(responses[1].Brand);
    }

    [Fact]
    public void ToListHttpResponse_WithEmptyList_ReturnsEmpty() {
        var models = new List<OpenFoodFactsProductModel>();

        var responses = models.ToListHttpResponse();

        Assert.Empty(responses);
    }

    [Fact]
    public void ToHttpResponse_WithNullOptionalFields_MapsCorrectly() {
        var model = new OpenFoodFactsProductModel(
            "1234567890123",
            "Unknown Product",
            null, null, null, null, null, null, null, null);

        var response = model.ToHttpResponse();

        Assert.NotNull(response);
        Assert.Equal("Unknown Product", response.Name);
        Assert.Null(response.Brand);
        Assert.Null(response.Category);
        Assert.Null(response.ImageUrl);
        Assert.Null(response.CaloriesPer100G);
        Assert.Null(response.ProteinsPer100G);
    }
}
