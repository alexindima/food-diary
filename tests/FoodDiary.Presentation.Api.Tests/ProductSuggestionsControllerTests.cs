using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Products.Models;
using FoodDiary.Application.Products.Queries.SearchProductSuggestions;
using FoodDiary.Mediator;
using FoodDiary.Presentation.Api.Features.Products;
using FoodDiary.Presentation.Api.Features.Products.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class ProductSuggestionsControllerTests {
    [Fact]
    public async Task SearchSuggestions_SendsSuggestionsQueryAndReturnsResponse() {
        var suggestion = new ProductSearchSuggestionModel(
            Source: "usda",
            Name: "Apple",
            Brand: "Farm",
            Category: "Fruit",
            Barcode: "123",
            UsdaFdcId: 456,
            ImageUrl: "https://cdn.example/apple.png",
            CaloriesPer100G: 52,
            ProteinsPer100G: 0.3,
            FatsPer100G: 0.2,
            CarbsPer100G: 14,
            FiberPer100G: 2.4);
        IRequest<Result<IReadOnlyList<ProductSearchSuggestionModel>>>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success<IReadOnlyList<ProductSearchSuggestionModel>>([suggestion]), request => sentRequest = request);
        ProductSuggestionsController controller = CreateController(sender);

        IActionResult result = await controller.SearchSuggestions("apple", limit: 7);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        IReadOnlyList<ProductSearchSuggestionHttpResponse> response = Assert.IsAssignableFrom<IReadOnlyList<ProductSearchSuggestionHttpResponse>>(ok.Value);
        ProductSearchSuggestionHttpResponse item = Assert.Single(response);
        Assert.Equal("Apple", item.Name);
        SearchProductSuggestionsQuery query = Assert.IsType<SearchProductSuggestionsQuery>(sentRequest);
        Assert.Equal("apple", query.Search);
        Assert.Equal(7, query.Limit);
    }

    private static ProductSuggestionsController CreateController(ISender sender) =>
        new(sender) {
            ControllerContext = new ControllerContext {
                HttpContext = new DefaultHttpContext(),
            },
        };
}
