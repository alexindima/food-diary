using FoodDiary.Application.Abstractions.Ai.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Ai.Commands.AnalyzeFoodImage;
using FoodDiary.Application.Ai.Commands.CalculateFoodNutrition;
using FoodDiary.Application.Ai.Commands.ParseFoodText;
using FoodDiary.Mediator;
using FoodDiary.Presentation.Api.Features.Ai;
using FoodDiary.Presentation.Api.Features.Ai.Models;
using FoodDiary.Presentation.Api.Features.Ai.Requests;
using FoodDiary.Presentation.Api.Features.Ai.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class AiFoodControllerTests {
    [Fact]
    public async Task AnalyzeFood_SendsVisionCommandAndReturnsResponse() {
        FoodVisionModel model = new([new FoodVisionItemModel("egg", "egg", 2, "pcs", 0.9m)], "vision notes");
        RecordingSender sender = new(Result.Success(model));
        AiFoodController controller = CreateController(sender);
        var userId = Guid.NewGuid();
        var imageAssetId = Guid.NewGuid();
        FoodVisionHttpRequest request = new(imageAssetId, "Dinner plate");

        IActionResult result = await controller.AnalyzeFood(userId, request);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        FoodVisionHttpResponse response = Assert.IsType<FoodVisionHttpResponse>(ok.Value);
        Assert.Equal("vision notes", response.Notes);
        FoodVisionItemHttpModel item = Assert.Single(response.Items);
        Assert.Equal("egg", item.NameEn);

        AnalyzeFoodImageCommand command = Assert.IsType<AnalyzeFoodImageCommand>(sender.Request);
        Assert.Equal(userId, command.UserId);
        Assert.Equal(imageAssetId, command.ImageAssetId);
        Assert.Equal("Dinner plate", command.Description);
    }

    [Fact]
    public async Task ParseFoodText_SendsTextCommandAndReturnsResponse() {
        FoodVisionModel model = new([new FoodVisionItemModel("toast", "toast", 1, "slice", 0.8m)], "text notes");
        RecordingSender sender = new(Result.Success(model));
        AiFoodController controller = CreateController(sender);
        var userId = Guid.NewGuid();
        FoodTextHttpRequest request = new("toast");

        IActionResult result = await controller.ParseFoodText(userId, request);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        FoodVisionHttpResponse response = Assert.IsType<FoodVisionHttpResponse>(ok.Value);
        Assert.Equal("text notes", response.Notes);
        FoodVisionItemHttpModel item = Assert.Single(response.Items);
        Assert.Equal("toast", item.NameEn);

        ParseFoodTextCommand command = Assert.IsType<ParseFoodTextCommand>(sender.Request);
        Assert.Equal(userId, command.UserId);
        Assert.Equal("toast", command.Text);
    }

    [Fact]
    public async Task CalculateNutrition_SendsNutritionCommandAndReturnsResponse() {
        FoodNutritionModel model = new(
            Calories: 300,
            Protein: 20,
            Fat: 10,
            Carbs: 30,
            Fiber: 5,
            Alcohol: 0,
            Items: [new FoodNutritionItemModel("egg", 2, "pcs", 160, 12, 10, 1, 0, 0)],
            Notes: "nutrition notes");
        RecordingSender sender = new(Result.Success(model));
        AiFoodController controller = CreateController(sender);
        var userId = Guid.NewGuid();
        FoodNutritionHttpRequest request = new([
            new FoodVisionItemHttpModel("egg", "egg", 2, "pcs", 0.9m),
        ]);

        IActionResult result = await controller.CalculateNutrition(userId, request);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        FoodNutritionHttpResponse response = Assert.IsType<FoodNutritionHttpResponse>(ok.Value);
        Assert.Equal(300, response.Calories);
        Assert.Equal("nutrition notes", response.Notes);
        FoodNutritionItemHttpResponse item = Assert.Single(response.Items);
        Assert.Equal("egg", item.Name);

        CalculateFoodNutritionCommand command = Assert.IsType<CalculateFoodNutritionCommand>(sender.Request);
        Assert.Equal(userId, command.UserId);
        FoodVisionItemModel sentItem = Assert.Single(command.Items);
        Assert.Equal("egg", sentItem.NameEn);
    }

    private static AiFoodController CreateController(ISender sender) =>
        new(sender, NullLogger<AiFoodController>.Instance) {
            ControllerContext = new ControllerContext {
                HttpContext = new DefaultHttpContext(),
            },
        };

    [ExcludeFromCodeCoverage]
    private sealed class RecordingSender(object response) : ISender {
        public object? Request { get; private set; }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default) {
            Request = request;
            return Task.FromResult((TResponse)response);
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest {
            Request = request;
            return Task.CompletedTask;
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default) {
            Request = request;
            return Task.FromResult<object?>(response);
        }

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
            IStreamRequest<TResponse> request,
            CancellationToken cancellationToken = default) {
            throw new NotSupportedException();
        }

        public IAsyncEnumerable<object?> CreateStream(
            object request,
            CancellationToken cancellationToken = default) {
            throw new NotSupportedException();
        }
    }
}
