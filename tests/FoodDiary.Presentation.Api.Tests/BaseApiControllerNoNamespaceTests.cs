using System.Diagnostics;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Mediator;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

#pragma warning disable MA0047
[ExcludeFromCodeCoverage]
public sealed class BaseApiControllerNoNamespaceTests {
    [Fact]
    public async Task HandleObservedCreated_WhenControllerNamespaceIsMissing_UsesUnknownFeatureName() {
        var request = new NoNamespaceCreatedRequest();
        StubSender mediator = new StubSender()
            .Register(request, Result.Success(new NoNamespaceCreatedModel(Guid.Parse("55555555-5555-5555-5555-555555555555"))));
        var controller = new NoNamespaceController(mediator) {
            ControllerContext = new ControllerContext {
                HttpContext = new DefaultHttpContext(),
            },
        };
        using var listener = new TestActivityListener(PresentationApiTelemetry.TelemetryName);

        _ = await controller.HandleObservedCreatedPublic(
            request,
            "GetById",
            static value => new { id = value.Id },
            static value => new { value.Id });

        Activity activity = Assert.Single(listener.CompletedActivitiesSnapshot, static item => string.Equals(item.OperationName, "test.no-namespace-created", StringComparison.Ordinal));
        Assert.Equal("Unknown", activity.GetTagItem("fooddiary.presentation.feature"));
    }

    [ExcludeFromCodeCoverage]
    private sealed class NoNamespaceController(ISender mediator) : BaseApiController(mediator) {
        public Task<IActionResult> HandleObservedCreatedPublic<TResponse, THttpResponse>(
            IRequest<Result<TResponse>> request,
            string actionName,
            Func<TResponse, object?> routeValues,
            Func<TResponse, THttpResponse> map) =>
            HandleObservedCreated(request, actionName, routeValues, map, NullLogger.Instance, "test.no-namespace-created");
    }

    [ExcludeFromCodeCoverage]
    private sealed record NoNamespaceCreatedModel(Guid Id);

    [ExcludeFromCodeCoverage]
    private sealed record NoNamespaceCreatedRequest : IRequest<Result<NoNamespaceCreatedModel>>;

    [ExcludeFromCodeCoverage]
    private sealed class StubSender : ISender {
        private readonly Dictionary<object, object> _responses = [];

        public StubSender Register<TResponse>(IRequest<TResponse> request, TResponse response) {
            _responses[request] = response!;
            return this;
        }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default) {
            return Task.FromResult((TResponse)_responses[request]!);
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest =>
            Task.CompletedTask;

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
        private readonly List<Activity> _completedActivities = [];

        public TestActivityListener(string sourceName) {
            _listener = new ActivityListener {
                ShouldListenTo = source => string.Equals(source.Name, sourceName, StringComparison.Ordinal),
                Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStopped = activity => _completedActivities.Add(activity),
            };
            ActivitySource.AddActivityListener(_listener);
        }

        public Activity[] CompletedActivitiesSnapshot => [.. _completedActivities];

        public void Dispose() {
            _listener.Dispose();
        }
    }
}
#pragma warning restore MA0047
