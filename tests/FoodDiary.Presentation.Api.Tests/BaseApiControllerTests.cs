using System.Diagnostics;
using System.Collections.Concurrent;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Extensions;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FoodDiary.Presentation.Api.Tests;

public sealed class BaseApiControllerTests {
    [Fact]
    public async Task HandleOk_ReturnsMappedOkResult() {
        var request = new TestOkRequest();
        var mediator = new StubSender()
            .Register(request, Result.Success("value"));
        var controller = CreateController(mediator);

        var result = await controller.HandleOkPublic(request, static value => value.ToUpperInvariant());

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("VALUE", ok.Value);
    }

    [Fact]
    public async Task HandleCreated_ReturnsCreatedAtActionResult() {
        var request = new TestCreatedRequest();
        var mediator = new StubSender()
            .Register(request, Result.Success(new CreatedModel(Guid.Parse("11111111-1111-1111-1111-111111111111"))));
        var controller = CreateController(mediator);

        var result = await controller.HandleCreatedPublic(
            request,
            "GetById",
            static value => new { id = value.Id },
            static value => new { value.Id });

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal("GetById", created.ActionName);
        Assert.Equal(Guid.Parse("11111111-1111-1111-1111-111111111111"), created.RouteValues!["id"]);
    }

    [Fact]
    public async Task HandleNoContent_ReturnsNoContentResult() {
        var request = new TestNoContentRequest();
        var mediator = new StubSender()
            .Register(request, Result.Success());
        var controller = CreateController(mediator);

        var result = await controller.HandleNoContentPublic(request);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task HandleOk_MapsFailureThroughStandardApiErrorContract() {
        var request = new TestOkRequest();
        var mediator = new StubSender()
            .Register(request, Result.Failure<string>(Errors.Validation.Invalid("Email", "Invalid email format")));
        var controller = CreateController(mediator);

        var result = await controller.HandleOkPublic(request, static value => value);

        var objectResult = Assert.IsType<ObjectResult>(result);
        var payload = Assert.IsType<ApiErrorHttpResponse>(objectResult.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
        Assert.NotNull(payload.Errors);
        Assert.Contains("email", payload.Errors.Keys);
        Assert.Equal("trace-base-controller", payload.TraceId);
    }

    [Fact]
    public async Task HandleObservedOk_CreatesPresentationActivity() {
        var request = new TestOkRequest();
        var mediator = new StubSender()
            .Register(request, Result.Success("value"));
        var controller = CreateController(mediator);
        using var listener = new TestActivityListener(PresentationApiTelemetry.TelemetryName);

        _ = await controller.HandleObservedOkPublic(request, static value => value.ToUpperInvariant(), NullLogger.Instance, "test.operation", Guid.Parse("33333333-3333-3333-3333-333333333333"));

        var activity = Assert.Single(listener.CompletedActivitiesSnapshot, static item => item.OperationName == "test.operation");
        Assert.Equal("test.operation", activity.OperationName);
        Assert.Equal("TestController", activity.GetTagItem("fooddiary.presentation.controller"));
        Assert.Equal("Unknown", activity.GetTagItem("fooddiary.presentation.feature"));
        Assert.Equal("success", activity.GetTagItem("fooddiary.presentation.outcome"));
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

        public Task<IActionResult> HandleObservedOkPublic<TResponse, THttpResponse>(
            IRequest<Result<TResponse>> request,
            Func<TResponse, THttpResponse> map,
            ILogger logger,
            string operationName,
            Guid? userId = null) =>
            HandleObservedOk(request, map, logger, operationName, userId);
    }

    private sealed record CreatedModel(Guid Id);

    private sealed record TestOkRequest : IRequest<Result<string>>;

    private sealed record TestCreatedRequest : IRequest<Result<CreatedModel>>;

    private sealed record TestNoContentRequest : IRequest<Result>;

    private sealed class StubSender : ISender {
        private readonly Dictionary<object, object> _responses = new();

        public StubSender Register<TResponse>(IRequest<TResponse> request, TResponse response) {
            _responses[request] = response!;
            return this;
        }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default) {
            return Task.FromResult((TResponse)_responses[request]!);
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest {
            throw new NotSupportedException();
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

        public Activity[] CompletedActivitiesSnapshot => _completedActivities.ToArray();

        public void Dispose() {
            _listener.Dispose();
        }
    }
}
