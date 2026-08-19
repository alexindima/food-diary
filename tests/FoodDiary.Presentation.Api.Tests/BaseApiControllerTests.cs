using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Results;
using FoodDiary.Application.Export.Models;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class BaseApiControllerTests {
    [Fact]
    public async Task HandleOk_ReturnsMappedOkResult() {
        var request = new TestOkRequest();
        ISender mediator = CreateSender(request, Result.Success("value"));
        TestController controller = CreateController(mediator);

        IActionResult result = await controller.HandleOkPublic(request, static value => value.ToUpperInvariant());

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("VALUE", ok.Value);
    }

    [Fact]
    public async Task HandleOptional_ReturnsNoContentWhenMappedValueIsNull() {
        var request = new TestOptionalRequest();
        ISender mediator = CreateSender(request, Result.Success<string?>(value: null));
        TestController controller = CreateController(mediator);

        IActionResult result = await controller.HandleOptionalPublic(request, static value => value);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task HandleOptional_ReturnsMappedOkResultWhenValueExists() {
        var request = new TestOptionalRequest();
        ISender mediator = CreateSender(request, Result.Success<string?>("value"));
        TestController controller = CreateController(mediator);

        IActionResult result = await controller.HandleOptionalPublic(request, static value => value?.ToUpperInvariant());

        Assert.Equal("VALUE", Assert.IsType<OkObjectResult>(result).Value);
    }

    [Fact]
    public async Task HandleCreated_ReturnsCreatedAtActionResult() {
        var request = new TestCreatedRequest();
        ISender mediator = CreateSender(
            request,
            Result.Success(new CreatedModel(Guid.Parse("11111111-1111-1111-1111-111111111111"))));
        TestController controller = CreateController(mediator);

        IActionResult result = await controller.HandleCreatedPublic(
            request,
            "GetById",
            static value => new { id = value.Id },
            static value => new { value.Id });

        CreatedAtActionResult created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal("GetById", created.ActionName);
        Assert.Equal(Guid.Parse("11111111-1111-1111-1111-111111111111"), created.RouteValues!["id"]);
    }

    [Fact]
    public async Task HandleNoContent_ReturnsNoContentResult() {
        var request = new TestNoContentRequest();
        ISender mediator = CreateSender(request, Result.Success());
        TestController controller = CreateController(mediator);

        IActionResult result = await controller.HandleNoContentPublic(request);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Send_UsesRequestAbortedCancellationToken() {
        var request = new TestVoidRequest();
        ISender mediator = Substitute.For<ISender>();
        mediator.Send(request, Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        TestController controller = CreateController(mediator);
        using var cts = new CancellationTokenSource();
        controller.HttpContext.RequestAborted = cts.Token;

        await controller.SendPublic(request);

        await mediator.Received(1).Send(request, cts.Token);
    }

    [Fact]
    public async Task HandleAccepted_ReturnsMappedAcceptedResult() {
        var request = new TestOkRequest();
        ISender mediator = CreateSender(request, Result.Success("value"));
        TestController controller = CreateController(mediator);

        IActionResult result = await controller.HandleAcceptedPublic(request, static value => value.ToUpperInvariant());

        AcceptedResult accepted = Assert.IsType<AcceptedResult>(result);
        Assert.Equal("VALUE", accepted.Value);
    }

    [Fact]
    public async Task HandleFile_ReturnsFileResult() {
        byte[] content = [1, 2, 3];
        var request = new TestFileRequest();
        ISender mediator = CreateSender(
            request,
            Result.Success(new FileExportResult(content, "text/csv", "export.csv")));
        TestController controller = CreateController(mediator);

        IActionResult result = await controller.HandleFilePublic(request);

        FileContentResult file = Assert.IsType<FileContentResult>(result);
        Assert.Equal(content, file.FileContents);
        Assert.Equal("text/csv", file.ContentType);
        Assert.Equal("export.csv", file.FileDownloadName);
    }

    [Fact]
    public async Task HandleOk_MapsFailureThroughStandardApiErrorContract() {
        var request = new TestOkRequest();
        ISender mediator = CreateSender(
            request,
            Result.Failure<string>(Errors.Validation.Invalid("Email", "Invalid email format")));
        TestController controller = CreateController(mediator);

        IActionResult result = await controller.HandleOkPublic(request, static value => value);

        ObjectResult objectResult = Assert.IsType<ObjectResult>(result);
        ApiErrorHttpResponse payload = Assert.IsType<ApiErrorHttpResponse>(objectResult.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
        Assert.NotNull(payload.Errors);
        Assert.Contains("email", payload.Errors.Keys, StringComparer.Ordinal);
        Assert.Equal("trace-base-controller", payload.TraceId);
    }

    private static TestController CreateController(ISender mediator) {
        var controller = new TestController(mediator) {
            ControllerContext = new ControllerContext {
                HttpContext = new DefaultHttpContext(),
            },
        };
        controller.ControllerContext.HttpContext.TraceIdentifier = "trace-base-controller";

        return controller;
    }

    private static ISender CreateSender<TResponse>(IRequest<TResponse> request, TResponse response) {
        ISender mediator = Substitute.For<ISender>();
        mediator.Send(request, Arg.Any<CancellationToken>()).Returns(Task.FromResult(response));
        return mediator;
    }

    [ExcludeFromCodeCoverage]
    private sealed class TestController(ISender mediator) : BaseApiController(mediator) {
        public Task<IActionResult> HandleOkPublic<TResponse, THttpResponse>(
            IRequest<Result<TResponse>> request,
            Func<TResponse, THttpResponse> map) =>
            HandleOk(request, map);

        public Task<IActionResult> HandleCreatedPublic<TResponse, THttpResponse>(
            IRequest<Result<TResponse>> request,
            string actionName,
            Func<TResponse, object?> routeValues,
            Func<TResponse, THttpResponse> map) =>
            HandleCreated(request, actionName, routeValues, map);

        public Task<IActionResult> HandleOptionalPublic<TResponse, THttpResponse>(
            IRequest<Result<TResponse>> request,
            Func<TResponse, THttpResponse?> map)
            where THttpResponse : class =>
            HandleOptional(request, map);

        public Task<IActionResult> HandleNoContentPublic(IRequest<Result> request) =>
            HandleNoContent(request);

        public Task SendPublic(IRequest request) =>
            Send(request);

        public Task<IActionResult> HandleAcceptedPublic<TResponse, THttpResponse>(
            IRequest<Result<TResponse>> request,
            Func<TResponse, THttpResponse> map) =>
            HandleAccepted(request, map);

        public Task<IActionResult> HandleFilePublic(IRequest<Result<FileExportResult>> request) =>
            HandleFile(request);

    }

    [ExcludeFromCodeCoverage]
    private sealed record CreatedModel(Guid Id);

    [ExcludeFromCodeCoverage]
    private sealed record TestOkRequest : IRequest<Result<string>>;

    [ExcludeFromCodeCoverage]
    private sealed record TestOptionalRequest : IRequest<Result<string?>>;

    [ExcludeFromCodeCoverage]
    private sealed record TestCreatedRequest : IRequest<Result<CreatedModel>>;

    [ExcludeFromCodeCoverage]
    private sealed record TestNoContentRequest : IRequest<Result>;

    [ExcludeFromCodeCoverage]
    private sealed record TestVoidRequest : IRequest;

    [ExcludeFromCodeCoverage]
    private sealed record TestFileRequest : IRequest<Result<FileExportResult>>;

}
