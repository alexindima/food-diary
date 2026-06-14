using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.OpenFoodFacts.Models;
using FoodDiary.Application.OpenFoodFacts.Queries.SearchByBarcode;
using FoodDiary.Application.OpenFoodFacts.Queries.SearchProducts;
using FoodDiary.Presentation.Api.Features.OpenFoodFacts;
using FoodDiary.Presentation.Api.Features.OpenFoodFacts.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class OpenFoodFactsControllerTests {
    [Fact]
    public async Task SearchByBarcode_SendsQueryAndReturnsProduct() {
        OpenFoodFactsProductModel product = CreateProduct("4600000000001");
        RecordingSender sender = new(Result.Success<OpenFoodFactsProductModel?>(product));
        OpenFoodFactsController controller = CreateController(sender);

        IActionResult result = await controller.SearchByBarcode("4600000000001");

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        OpenFoodFactsProductHttpResponse response = Assert.IsType<OpenFoodFactsProductHttpResponse>(ok.Value);
        Assert.Equal("4600000000001", response.Barcode);
        SearchByBarcodeQuery query = Assert.IsType<SearchByBarcodeQuery>(sender.Request);
        Assert.Equal("4600000000001", query.Barcode);
    }

    [Fact]
    public async Task Search_SendsQueryAndReturnsProducts() {
        OpenFoodFactsProductModel product = CreateProduct("111");
        RecordingSender sender = new(Result.Success<IReadOnlyList<OpenFoodFactsProductModel>>([product]));
        OpenFoodFactsController controller = CreateController(sender);

        IActionResult result = await controller.Search("milk", limit: 15);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        IReadOnlyList<OpenFoodFactsProductHttpResponse> response = Assert.IsAssignableFrom<IReadOnlyList<OpenFoodFactsProductHttpResponse>>(ok.Value);
        OpenFoodFactsProductHttpResponse item = Assert.Single(response);
        Assert.Equal("111", item.Barcode);
        SearchOpenFoodFactsQuery query = Assert.IsType<SearchOpenFoodFactsQuery>(sender.Request);
        Assert.Equal("milk", query.Search);
        Assert.Equal(15, query.Limit);
    }

    private static OpenFoodFactsController CreateController(RecordingSender sender) =>
        new(sender) {
            ControllerContext = new ControllerContext {
                HttpContext = new DefaultHttpContext(),
            },
        };

    private static OpenFoodFactsProductModel CreateProduct(string barcode) =>
        new(
            barcode,
            "Milk",
            "Brand",
            "Dairy",
            "https://images.openfoodfacts.org/test.jpg",
            CaloriesPer100G: 60,
            ProteinsPer100G: 3.2,
            FatsPer100G: 3.2,
            CarbsPer100G: 4.7,
            FiberPer100G: 0);
}
