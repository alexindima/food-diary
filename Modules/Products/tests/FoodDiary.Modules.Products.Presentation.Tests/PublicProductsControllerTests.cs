using FoodDiary.Mediator;
using FoodDiary.Modules.Products.Application.Models;
using FoodDiary.Modules.Products.Application.Queries.GetPublicProduct;
using FoodDiary.Modules.Products.Presentation.Controllers;
using FoodDiary.Modules.Products.Presentation.Responses;
using FoodDiary.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Modules.Products.Presentation.Tests;

[ExcludeFromCodeCoverage]
public sealed class PublicProductsControllerTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetById_PreservesPublicNutritionAndOptionalMediaAndForwardsCancellation(bool hasMedia) {
        var id = Guid.NewGuid();
        string[] images = hasMedia ? ["https://example.test/first.jpg", "https://example.test/second.jpg"] : [];
        var model = new PublicProductModel(id, "Oat drink", "Farm", hasMedia ? images[0] : null,
            "Ml", 100, 102.25, 3.5, 4.25, 12.75, 2.5, 1.25) {
            Description = hasMedia ? "Unsweetened" : null,
            Images = images,
        };
        using var cancellation = new CancellationTokenSource();
        ISender sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<IRequest<Result<PublicProductModel>>>(), cancellation.Token).Returns(Result.Success(model));
        PublicProductsController controller = CreateController(sender, cancellation.Token);

        OkObjectResult result = Assert.IsType<OkObjectResult>(await controller.GetById(id));
        PublicProductHttpResponse response = Assert.IsType<PublicProductHttpResponse>(result.Value);

        Assert.Multiple(
            () => Assert.Equal(id, response.Id),
            () => Assert.Equal("Oat drink", response.Name),
            () => Assert.Equal("Farm", response.Brand),
            () => Assert.Equal(hasMedia ? images[0] : null, response.ImageUrl),
            () => Assert.Equal(hasMedia ? "Unsweetened" : null, response.Description),
            () => Assert.Equal(images, response.Images),
            () => Assert.Equal("Ml", response.BaseUnit),
            () => Assert.Equal(100, response.BaseAmount),
            () => Assert.Equal(102.25, response.Calories),
            () => Assert.Equal(3.5, response.Proteins),
            () => Assert.Equal(4.25, response.Fats),
            () => Assert.Equal(12.75, response.Carbs),
            () => Assert.Equal(2.5, response.Fiber),
            () => Assert.Equal(1.25, response.Alcohol));
        await sender.Received(1).Send(Arg.Is<IRequest<Result<PublicProductModel>>>(request =>
            request is GetPublicProductQuery && ((GetPublicProductQuery)request).ProductId.Value == id), cancellation.Token);
        Assert.Single(sender.ReceivedCalls());
    }

    [Fact]
    public async Task GetById_WhenThePublicProductIsUnavailable_ReturnsNotFound() {
        ISender sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<IRequest<Result<PublicProductModel>>>(), CancellationToken.None)
            .Returns(Result.Failure<PublicProductModel>(new Error("Product.NotFound", "Product is unavailable.", ErrorKind.NotFound)));

        ObjectResult response = Assert.IsAssignableFrom<ObjectResult>(await CreateController(sender, CancellationToken.None).GetById(Guid.NewGuid()));

        Assert.Equal(StatusCodes.Status404NotFound, response.StatusCode);
    }

    private static PublicProductsController CreateController(ISender sender, CancellationToken cancellationToken) => new(sender) {
        ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { RequestAborted = cancellationToken } },
    };
}
