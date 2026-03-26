using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Responses;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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
}
