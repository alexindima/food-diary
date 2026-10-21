using System.Text.Json;
using FoodDiary.Mediator;
using FoodDiary.Modules.Products.Application.Commands.ImportCatalogProduct;
using FoodDiary.Modules.Products.Application.Models;
using FoodDiary.Modules.Products.Application.Queries.ExportCatalogProducts;
using FoodDiary.Modules.Products.Presentation.Controllers;
using FoodDiary.Modules.Products.Presentation.Requests;
using FoodDiary.Modules.Products.Presentation.Responses;
using FoodDiary.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Modules.Products.Presentation.Tests;

[ExcludeFromCodeCoverage]
public sealed class AdminCatalogProductsControllerTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Export_PreservesTheWireFieldsAndRepositoryOrderIncludingAnEmptyCatalog(bool empty) {
        CatalogProductModel[] rows = empty ? [] : [
            new(Guid.NewGuid(), "Oats", "123", "Farm", "Solid", "Grain", "Whole grain", "https://example.test/oats.jpg", "G", 100, 45, 370, 12, 7, 60, 9, 0.5),
            new(Guid.NewGuid(), "Milk", Barcode: null, Brand: null, "Liquid", Category: null, Description: null, ImageUrl: null, "Ml", 100, 250, 60, 3, 2, 5, 0, 0),
        ];
        using var cancellation = new CancellationTokenSource();
        ISender sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<IRequest<Result<IReadOnlyList<CatalogProductModel>>>>(), cancellation.Token)
            .Returns(Result.Success<IReadOnlyList<CatalogProductModel>>(rows));

        OkObjectResult result = Assert.IsType<OkObjectResult>(await CreateController(sender, cancellation.Token).Export());
        IReadOnlyList<CatalogProductHttpResponse> response = Assert.IsAssignableFrom<IReadOnlyList<CatalogProductHttpResponse>>(result.Value);

        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        Assert.Equal(JsonSerializer.Serialize(rows, options), JsonSerializer.Serialize(response, options));
        await sender.Received(1).Send(Arg.Is<IRequest<Result<IReadOnlyList<CatalogProductModel>>>>(request => request is ExportCatalogProductsQuery), cancellation.Token);
        Assert.Single(sender.ReceivedCalls());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ImportAndPreview_ForwardTheCurrentUserAndModeAndPreserveValidationDetails(bool preview) {
        var user = Guid.NewGuid();
        var id = Guid.NewGuid();
        var request = new CatalogProductHttpRequest(id, "Oats", "123", "Farm", "Solid", "Grain", "Whole grain",
            "https://example.test/oats.jpg", "G", 100, 45, 370, 12, 7, 60, 9, 0.5);
        var expectedProduct = new CatalogProductModel(id, "Oats", "123", "Farm", "Solid", "Grain", "Whole grain",
            "https://example.test/oats.jpg", "G", 100, 45, 370, 12, 7, 60, 9, 0.5);
        string[] errors = ["Barcode already exists", "Review the catalog entry"];
        using var cancellation = new CancellationTokenSource();
        ISender sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<IRequest<Result<CatalogProductImportResult>>>(), cancellation.Token)
            .Returns(Result.Success(new CatalogProductImportResult(id, "invalid", errors)));
        AdminCatalogProductsController controller = CreateController(sender, cancellation.Token);

        IActionResult action = preview ? await controller.Preview(user, request) : await controller.Import(user, request);
        OkObjectResult result = Assert.IsType<OkObjectResult>(action);
        CatalogProductImportHttpResponse response = Assert.IsType<CatalogProductImportHttpResponse>(result.Value);

        Assert.Multiple(
            () => Assert.Equal(id, response.Id),
            () => Assert.Equal("invalid", response.Status),
            () => Assert.Equal(errors, response.Errors));
        await sender.Received(1).Send(Arg.Is<IRequest<Result<CatalogProductImportResult>>>(command =>
            command.Equals(new ImportCatalogProductCommand(user, expectedProduct, preview))), cancellation.Token);
        Assert.Single(sender.ReceivedCalls());
    }

    private static AdminCatalogProductsController CreateController(ISender sender, CancellationToken cancellationToken) => new(sender) {
        ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { RequestAborted = cancellationToken } },
    };
}
