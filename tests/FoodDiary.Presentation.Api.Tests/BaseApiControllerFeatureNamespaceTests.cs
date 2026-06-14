using System.Diagnostics;
using System.Collections.Concurrent;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Mediator;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace FoodDiary.Presentation.Api.Features.Coverage;

[ExcludeFromCodeCoverage]
public sealed class BaseApiControllerFeatureNamespaceTests {
    [Fact]
    public async Task HandleObservedOk_WhenControllerIsInFeaturesNamespace_UsesFeatureSegment() {
        var request = new TestRequest();
        StubSender mediator = new StubSender()
            .Register(request, Result.Success("value"));
        var controller = new CoverageController(mediator) {
            ControllerContext = new ControllerContext {
                HttpContext = new DefaultHttpContext(),
            },
        };
        using var listener = new TestActivityListener(PresentationApiTelemetry.TelemetryName);

        _ = await controller.HandleObservedOkPublic(request);

        Activity activity = Assert.Single(listener.CompletedActivitiesSnapshot, static item => string.Equals(item.OperationName, "test.feature-name", StringComparison.Ordinal));
        Assert.Equal("Coverage", activity.GetTagItem("fooddiary.presentation.feature"));
    }

    [ExcludeFromCodeCoverage]
    private sealed class CoverageController(ISender mediator) : BaseApiController(mediator) {
        public Task<IActionResult> HandleObservedOkPublic(IRequest<Result<string>> request) =>
            HandleObservedOk(request, static value => value, NullLogger.Instance, "test.feature-name");
    }

    [ExcludeFromCodeCoverage]
    private sealed record TestRequest : IRequest<Result<string>>;

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
        private readonly ConcurrentQueue<Activity> _completedActivities = new();

        public TestActivityListener(string sourceName) {
            _listener = new ActivityListener {
                ShouldListenTo = source => string.Equals(source.Name, sourceName, StringComparison.Ordinal),
                Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStopped = activity => _completedActivities.Enqueue(activity),
            };
            ActivitySource.AddActivityListener(_listener);
        }

        public Activity[] CompletedActivitiesSnapshot => [.. _completedActivities];

        public void Dispose() {
            _listener.Dispose();
        }
    }
}
