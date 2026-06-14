using System.Diagnostics;
using System.Collections.Concurrent;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Export.Models;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Extensions;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class BaseApiControllerTests {
    [Fact]
    public async Task HandleOk_ReturnsMappedOkResult() {
        var request = new TestOkRequest();
        StubSender mediator = new StubSender()
            .Register(request, Result.Success("value"));
        TestController controller = CreateController(mediator);

        IActionResult result = await controller.HandleOkPublic(request, static value => value.ToUpperInvariant());

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("VALUE", ok.Value);
    }

    [Fact]
    public async Task HandleCreated_ReturnsCreatedAtActionResult() {
        var request = new TestCreatedRequest();
        StubSender mediator = new StubSender()
            .Register(request, Result.Success(new CreatedModel(Guid.Parse("11111111-1111-1111-1111-111111111111"))));
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
        StubSender mediator = new StubSender()
            .Register(request, Result.Success());
        TestController controller = CreateController(mediator);

        IActionResult result = await controller.HandleNoContentPublic(request);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Send_UsesRequestAbortedCancellationToken() {
        var request = new TestVoidRequest();
        var mediator = new StubSender();
        TestController controller = CreateController(mediator);
        using var cts = new CancellationTokenSource();
        controller.HttpContext.RequestAborted = cts.Token;

        await controller.SendPublic(request);

        Assert.Same(request, mediator.VoidRequest);
        Assert.Equal(cts.Token, mediator.CancellationToken);
    }

    [Fact]
    public async Task HandleAccepted_ReturnsMappedAcceptedResult() {
        var request = new TestOkRequest();
        StubSender mediator = new StubSender()
            .Register(request, Result.Success("value"));
        TestController controller = CreateController(mediator);

        IActionResult result = await controller.HandleAcceptedPublic(request, static value => value.ToUpperInvariant());

        AcceptedResult accepted = Assert.IsType<AcceptedResult>(result);
        Assert.Equal("VALUE", accepted.Value);
    }

    [Fact]
    public async Task HandleFile_ReturnsFileResult() {
        byte[] content = [1, 2, 3];
        var request = new TestFileRequest();
        StubSender mediator = new StubSender()
            .Register(request, Result.Success(new FileExportResult(content, "text/csv", "export.csv")));
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
        StubSender mediator = new StubSender()
            .Register(request, Result.Failure<string>(Errors.Validation.Invalid("Email", "Invalid email format")));
        TestController controller = CreateController(mediator);

        IActionResult result = await controller.HandleOkPublic(request, static value => value);

        ObjectResult objectResult = Assert.IsType<ObjectResult>(result);
        ApiErrorHttpResponse payload = Assert.IsType<ApiErrorHttpResponse>(objectResult.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
        Assert.NotNull(payload.Errors);
        Assert.Contains("email", payload.Errors.Keys);
        Assert.Equal("trace-base-controller", payload.TraceId);
    }

    [Fact]
    public async Task HandleObservedOk_CreatesPresentationActivity() {
        var request = new TestOkRequest();
        StubSender mediator = new StubSender()
            .Register(request, Result.Success("value"));
        TestController controller = CreateController(mediator);
        using var listener = new TestActivityListener(PresentationApiTelemetry.TelemetryName);

        _ = await controller.HandleObservedOkPublic(request, static value => value.ToUpperInvariant(), NullLogger.Instance, "test.operation", Guid.Parse("33333333-3333-3333-3333-333333333333"));

        Activity activity = Assert.Single(listener.CompletedActivitiesSnapshot, static item => string.Equals(item.OperationName, "test.operation", StringComparison.Ordinal));
        Assert.Equal("test.operation", activity.OperationName);
        Assert.Equal("TestController", activity.GetTagItem("fooddiary.presentation.controller"));
        Assert.Equal("Unknown", activity.GetTagItem("fooddiary.presentation.feature"));
        Assert.Equal("success", activity.GetTagItem("fooddiary.presentation.outcome"));
    }

    [Fact]
    public async Task HandleObservedCreated_ReturnsCreatedAtActionResultAndCreatesPresentationActivity() {
        var request = new TestCreatedRequest();
        StubSender mediator = new StubSender()
            .Register(request, Result.Success(new CreatedModel(Guid.Parse("22222222-2222-2222-2222-222222222222"))));
        TestController controller = CreateController(mediator);
        controller.HttpContext.SetEndpoint(new Endpoint(_ => Task.CompletedTask, EndpointMetadataCollection.Empty, "GET /test/{id}"));
        using var listener = new TestActivityListener(PresentationApiTelemetry.TelemetryName);

        IActionResult result = await controller.HandleObservedCreatedPublic(
            request,
            "GetById",
            static value => new { id = value.Id },
            static value => new { value.Id },
            NullLogger.Instance,
            "test.created",
            Guid.Parse("44444444-4444-4444-4444-444444444444"));

        CreatedAtActionResult created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal("GetById", created.ActionName);
        Assert.Equal(Guid.Parse("22222222-2222-2222-2222-222222222222"), created.RouteValues!["id"]);
        Activity activity = Assert.Single(listener.CompletedActivitiesSnapshot, static item => string.Equals(item.OperationName, "test.created", StringComparison.Ordinal));
        Assert.Equal("success", activity.GetTagItem("fooddiary.presentation.outcome"));
        Assert.Equal("GET /test/{id}", activity.GetTagItem("http.route"));
    }

    [Fact]
    public async Task HandleObservedNoContent_WithFailure_ReturnsApiErrorAndCreatesFailureActivity() {
        var request = new TestNoContentRequest();
        StubSender mediator = new StubSender()
            .Register(request, Result.Failure(Errors.Validation.Invalid("Name", "Name is required.")));
        TestController controller = CreateController(mediator);
        using var listener = new TestActivityListener(PresentationApiTelemetry.TelemetryName);

        IActionResult result = await controller.HandleObservedNoContentPublic(request, NullLogger.Instance, "test.failure");

        ObjectResult objectResult = Assert.IsType<ObjectResult>(result);
        ApiErrorHttpResponse response = Assert.IsType<ApiErrorHttpResponse>(objectResult.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
        Assert.Equal("Validation.Invalid", response.Error);
        Activity activity = Assert.Single(listener.CompletedActivitiesSnapshot, static item => string.Equals(item.OperationName, "test.failure", StringComparison.Ordinal));
        Assert.Equal("failure", activity.GetTagItem("fooddiary.presentation.outcome"));
        Assert.Equal(ActivityStatusCode.Error, activity.Status);
        Assert.Equal("Validation.Invalid", activity.GetTagItem("error.type"));
    }

    [Fact]
    public async Task HandleObservedOk_WithoutActivityListener_ReturnsResult() {
        var request = new TestOkRequest();
        StubSender mediator = new StubSender()
            .Register(request, Result.Success("value"));
        TestController controller = CreateController(mediator);

        IActionResult result = await controller.HandleObservedOkPublic(request, static value => value, NullLogger.Instance, "test.no-activity");

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("value", ok.Value);
    }

    [Fact]
    public async Task HandleObservedNoContent_WithFailureAndWithoutActivityListener_ReturnsApiError() {
        var request = new TestNoContentRequest();
        StubSender mediator = new StubSender()
            .Register(request, Result.Failure(Errors.Validation.Invalid("Name", "Name is required.")));
        TestController controller = CreateController(mediator);

        IActionResult result = await controller.HandleObservedNoContentPublic(request, NullLogger.Instance, "test.failure-no-activity");

        ObjectResult objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
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

        public Task<IActionResult> HandleObservedOkPublic<TResponse, THttpResponse>(
            IRequest<Result<TResponse>> request,
            Func<TResponse, THttpResponse> map,
            ILogger logger,
            string operationName,
            Guid? userId = null) =>
            HandleObservedOk(request, map, logger, operationName, userId);

        public Task<IActionResult> HandleObservedCreatedPublic<TResponse, THttpResponse>(
            IRequest<Result<TResponse>> request,
            string actionName,
            Func<TResponse, object?> routeValues,
            Func<TResponse, THttpResponse> map,
            ILogger logger,
            string operationName,
            Guid? userId = null) =>
            HandleObservedCreated(request, actionName, routeValues, map, logger, operationName, userId);

        public Task<IActionResult> HandleObservedNoContentPublic(
            IRequest<Result> request,
            ILogger logger,
            string operationName,
            Guid? userId = null) =>
            HandleObservedNoContent(request, logger, operationName, userId);
    }

    [ExcludeFromCodeCoverage]
    private sealed record CreatedModel(Guid Id);

    [ExcludeFromCodeCoverage]
    private sealed record TestOkRequest : IRequest<Result<string>>;

    [ExcludeFromCodeCoverage]
    private sealed record TestCreatedRequest : IRequest<Result<CreatedModel>>;

    [ExcludeFromCodeCoverage]
    private sealed record TestNoContentRequest : IRequest<Result>;

    [ExcludeFromCodeCoverage]
    private sealed record TestVoidRequest : IRequest;

    [ExcludeFromCodeCoverage]
    private sealed record TestFileRequest : IRequest<Result<FileExportResult>>;

    [ExcludeFromCodeCoverage]
    private sealed class StubSender : ISender {
        private readonly Dictionary<object, object> _responses = [];

        public IRequest? VoidRequest { get; private set; }
        public CancellationToken CancellationToken { get; private set; }

        public StubSender Register<TResponse>(IRequest<TResponse> request, TResponse response) {
            _responses[request] = response!;
            return this;
        }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default) {
            return Task.FromResult((TResponse)_responses[request]!);
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest {
            VoidRequest = request;
            CancellationToken = cancellationToken;
            return Task.CompletedTask;
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default) {
            return Task.FromResult<object?>(_responses[request]);
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

    [ExcludeFromCodeCoverage]
    private sealed class TestActivityListener : IDisposable {
        private readonly ActivityListener _listener;

        public TestActivityListener(string sourceName) {
            _listener = new ActivityListener {
                ShouldListenTo = source => string.Equals(source.Name, sourceName, StringComparison.Ordinal),
                Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStopped = activity => _completedActivities.Enqueue(activity),
            };
            ActivitySource.AddActivityListener(_listener);
        }

        private readonly ConcurrentQueue<Activity> _completedActivities = new();

        public Activity[] CompletedActivitiesSnapshot => [.. _completedActivities];

        public void Dispose() {
            _listener.Dispose();
        }
    }
}
