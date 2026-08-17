using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;
using System.Reflection.Emit;
using FoodDiary.Presentation.Api.Extensions;
using FoodDiary.Presentation.Api.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace FoodDiary.Presentation.Api.Tests;

[Collection(PresentationTelemetryCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class TelemetryActionFilterTests {
    [Fact]
    public async Task OnResourceExecutionAsync_WithSuccessfulResult_RecordsFinalStatusOnce() {
        using var metrics = new PresentationMetricListener();
        using var activities = new PresentationActivityListener();
        var logger = new RecordingLogger<TelemetryActionFilter>();
        var filter = new TelemetryActionFilter(logger);
        ResourceExecutingContext context = CreateResourceExecutingContext("Get");
        bool nextCalled = false;

        await filter.OnResourceExecutionAsync(context, () => {
            nextCalled = true;
            context.HttpContext.Response.StatusCode = StatusCodes.Status201Created;
            return Task.FromResult(new ResourceExecutedContext(context, []));
        });

        MetricMeasurement operation = Assert.Single(metrics.Operations);
        MetricMeasurement duration = Assert.Single(metrics.Durations);
        Activity activity = Assert.Single(activities.Completed);
        Assert.Multiple(
            () => Assert.True(nextCalled),
            () => Assert.Equal(int.MinValue, filter.Order),
            () => Assert.Empty(metrics.Failures),
            () => Assert.Empty(logger.Entries),
            () => Assert.Equal(1, operation.Value),
            () => Assert.Equal("Unknown", operation.Tags["fooddiary.presentation.feature"]),
            () => Assert.Equal("TelemetryProbeController.Get", operation.Tags["fooddiary.presentation.operation"]),
            () => Assert.Equal("success", operation.Tags["fooddiary.presentation.outcome"]),
            () => Assert.True(duration.Value >= 0),
            () => Assert.Equal("success", activity.GetTagItem("fooddiary.presentation.outcome")),
            () => Assert.Equal(StatusCodes.Status201Created, activity.GetTagItem("http.response.status_code")));
    }

    [Fact]
    public async Task OnResultExecutionAsync_WithAuthorizationShortCircuit_RecordsFinalFailure() {
        using var metrics = new PresentationMetricListener();
        using var activities = new PresentationActivityListener();
        var logger = new RecordingLogger<TelemetryActionFilter>();
        var filter = new TelemetryActionFilter(logger);
        ResultExecutingContext context = CreateResultExecutingContext("Get", new UnauthorizedResult());

        await filter.OnResultExecutionAsync(context, () => {
            context.HttpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.FromResult(new ResultExecutedContext(context, [], context.Result, context.Controller));
        });

        MetricMeasurement operation = Assert.Single(metrics.Operations);
        MetricMeasurement failure = Assert.Single(metrics.Failures);
        LogEntry log = Assert.Single(logger.Entries);
        Activity activity = Assert.Single(activities.Completed);
        Assert.Multiple(
            () => Assert.Equal("failure", operation.Tags["fooddiary.presentation.outcome"]),
            () => Assert.Equal("HttpStatus_401", failure.Tags["error.code"]),
            () => Assert.Equal("Unknown", failure.Tags["fooddiary.presentation.feature"]),
            () => Assert.Equal(LogLevel.Information, log.Level),
            () => Assert.Equal(ActivityStatusCode.Unset, activity.Status));
    }

    [Fact]
    public async Task OnResourceExecutionAsync_WithServerFailure_RecordsFailureAndMarksActivityAsError() {
        using var metrics = new PresentationMetricListener();
        using var activities = new PresentationActivityListener();
        var logger = new RecordingLogger<TelemetryActionFilter>();
        var filter = new TelemetryActionFilter(logger);
        ResourceExecutingContext context = CreateResourceExecutingContext("Post");

        await filter.OnResourceExecutionAsync(context, () => {
            context.HttpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            return Task.FromResult(new ResourceExecutedContext(context, []));
        });

        MetricMeasurement failure = Assert.Single(metrics.Failures);
        Activity activity = Assert.Single(activities.Completed);
        LogEntry log = Assert.Single(logger.Entries);
        Assert.Multiple(
            () => Assert.Equal("HttpStatus_503", failure.Tags["error.code"]),
            () => Assert.Equal(ActivityStatusCode.Error, activity.Status),
            () => Assert.Equal(LogLevel.Warning, log.Level));
    }

    [Fact]
    public async Task OnResourceExecutionAsync_WithUnhandledException_RecordsUnhandledFailure() {
        using var metrics = new PresentationMetricListener();
        using var activities = new PresentationActivityListener();
        var logger = new RecordingLogger<TelemetryActionFilter>();
        var filter = new TelemetryActionFilter(logger);
        ResourceExecutingContext context = CreateResourceExecutingContext("Post");
        var exception = new InvalidOperationException("boom");

        await filter.OnResourceExecutionAsync(context, () => Task.FromResult(
            new ResourceExecutedContext(context, []) {
                Exception = exception,
            }));

        MetricMeasurement failure = Assert.Single(metrics.Failures);
        Activity activity = Assert.Single(activities.Completed);
        LogEntry log = Assert.Single(logger.Entries);
        Assert.Multiple(
            () => Assert.Equal("UnhandledException", failure.Tags["error.code"]),
            () => Assert.Equal(ActivityStatusCode.Error, activity.Status),
            () => Assert.Equal(typeof(InvalidOperationException).FullName, activity.GetTagItem("error.type")),
            () => Assert.Equal(LogLevel.Warning, log.Level));
    }

    [Fact]
    public async Task OnResourceExecutionAsync_WithHandledException_UsesFinalResponseStatus() {
        using var metrics = new PresentationMetricListener();
        using var activities = new PresentationActivityListener();
        var filter = new TelemetryActionFilter(new RecordingLogger<TelemetryActionFilter>());
        ResourceExecutingContext context = CreateResourceExecutingContext("Get");

        await filter.OnResourceExecutionAsync(context, () => {
            context.HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            return Task.FromResult(new ResourceExecutedContext(context, []) {
                Exception = new InvalidOperationException("handled"),
                ExceptionHandled = true,
            });
        });

        MetricMeasurement failure = Assert.Single(metrics.Failures);
        Activity activity = Assert.Single(activities.Completed);
        Assert.Multiple(
            () => Assert.Equal("HttpStatus_400", failure.Tags["error.code"]),
            () => Assert.Null(activity.GetTagItem("error.type")),
            () => Assert.Equal(StatusCodes.Status400BadRequest, activity.GetTagItem("http.response.status_code")));
    }

    [Fact]
    public async Task OnResourceExecutionAsync_WhenDelegateThrows_RecordsFailureAndRethrows() {
        using var metrics = new PresentationMetricListener();
        using var activities = new PresentationActivityListener();
        var filter = new TelemetryActionFilter(new RecordingLogger<TelemetryActionFilter>());
        ResourceExecutingContext context = CreateResourceExecutingContext("Post");
        var exception = new InvalidOperationException("boom");

        InvalidOperationException thrown = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            filter.OnResourceExecutionAsync(
                context,
                () => Task.FromException<ResourceExecutedContext>(exception)));

        Assert.Multiple(
            () => Assert.Same(exception, thrown),
            () => Assert.Single(metrics.Operations),
            () => Assert.Equal("UnhandledException", Assert.Single(metrics.Failures).Tags["error.code"]),
            () => Assert.False(context.HttpContext.Items.Any()),
            () => Assert.Equal(ActivityStatusCode.Error, Assert.Single(activities.Completed).Status));
    }

    [Fact]
    public async Task OnResultExecutionAsync_WhenDelegateThrows_RecordsFailureAndRethrows() {
        using var metrics = new PresentationMetricListener();
        using var activities = new PresentationActivityListener();
        var filter = new TelemetryActionFilter(new RecordingLogger<TelemetryActionFilter>());
        ResultExecutingContext context = CreateResultExecutingContext("Get", new OkResult());
        var exception = new InvalidOperationException("boom");

        InvalidOperationException thrown = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            filter.OnResultExecutionAsync(
                context,
                () => Task.FromException<ResultExecutedContext>(exception)));

        Assert.Multiple(
            () => Assert.Same(exception, thrown),
            () => Assert.Single(metrics.Operations),
            () => Assert.Equal("UnhandledException", Assert.Single(metrics.Failures).Tags["error.code"]),
            () => Assert.Equal(ActivityStatusCode.Error, Assert.Single(activities.Completed).Status));
    }

    [Fact]
    public async Task ResourceAndResultFilters_WhenBothRun_RecordOperationOnlyOnce() {
        using var metrics = new PresentationMetricListener();
        using var activities = new PresentationActivityListener();
        var filter = new TelemetryActionFilter(new RecordingLogger<TelemetryActionFilter>());
        ResourceExecutingContext resourceContext = CreateResourceExecutingContext("Get");

        await filter.OnResourceExecutionAsync(resourceContext, async () => {
            ResultExecutingContext resultContext = CreateResultExecutingContext(
                resourceContext.ActionDescriptor,
                resourceContext.HttpContext,
                new OkResult());
            await filter.OnResultExecutionAsync(resultContext, () => {
                resourceContext.HttpContext.Response.StatusCode = StatusCodes.Status200OK;
                return Task.FromResult(new ResultExecutedContext(
                    resultContext,
                    [],
                    resultContext.Result,
                    resultContext.Controller));
            });
            return new ResourceExecutedContext(resourceContext, []);
        });

        Assert.Multiple(
            () => Assert.Single(metrics.Operations),
            () => Assert.Single(metrics.Durations),
            () => Assert.Single(activities.Completed));
    }

    [Fact]
    public void ResolveFeature_HandlesNamespacesWithoutFeatureSegment() {
        Assert.Multiple(
            () => Assert.Equal("Unknown", ResolveFeature(CreateControllerType("NoNamespaceController"))),
            () => Assert.Equal("Unknown", ResolveFeature(CreateControllerType("Example.Features.ProbeController"))),
            () => Assert.Equal("Unknown", ResolveFeature(CreateControllerType("Example.Controllers.ProbeController"))),
            () => Assert.Equal("Meals", ResolveFeature(CreateControllerType("Example.Features.Meals.ProbeController"))));
    }

    private static string ResolveFeature(Type controllerType) {
        MethodInfo method = typeof(TelemetryActionFilter).GetMethod(
            "ResolveFeature",
            BindingFlags.Static | BindingFlags.NonPublic)!;
        return Assert.IsType<string>(method.Invoke(null, [controllerType]));
    }

    private static Type CreateControllerType(string typeName) {
        var assemblyName = new AssemblyName($"TelemetryActionFilterTests.Dynamic.{Guid.NewGuid():N}");
        var assembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        ModuleBuilder module = assembly.DefineDynamicModule("Main");
        TypeBuilder type = module.DefineType(typeName, TypeAttributes.Public);
        return type.CreateType()!;
    }

    private static ResourceExecutingContext CreateResourceExecutingContext(string actionName) {
        var httpContext = new DefaultHttpContext();
        ControllerActionDescriptor descriptor = CreateActionDescriptor(actionName);
        var actionContext = new ActionContext(httpContext, new RouteData(), descriptor);
        return new ResourceExecutingContext(actionContext, [], []);
    }

    private static ResultExecutingContext CreateResultExecutingContext(string actionName, IActionResult result) {
        var httpContext = new DefaultHttpContext();
        return CreateResultExecutingContext(CreateActionDescriptor(actionName), httpContext, result);
    }

    private static ResultExecutingContext CreateResultExecutingContext(
        ActionDescriptor descriptor,
        HttpContext httpContext,
        IActionResult result) =>
        new(
            new ActionContext(httpContext, new RouteData(), descriptor),
            [],
            result,
            new TelemetryProbeController());

    private static ControllerActionDescriptor CreateActionDescriptor(string actionName) =>
        new() {
            ActionName = actionName,
            ControllerName = "TelemetryProbe",
            ControllerTypeInfo = typeof(TelemetryProbeController).GetTypeInfo(),
            MethodInfo = typeof(object).GetMethod(nameof(ToString))!,
            RouteValues = new Dictionary<string, string?>(StringComparer.Ordinal) {
                ["action"] = actionName,
            },
        };

    [ExcludeFromCodeCoverage]
    private sealed record MetricMeasurement(long Value, IReadOnlyDictionary<string, object?> Tags);

    [ExcludeFromCodeCoverage]
    private sealed class PresentationMetricListener : IDisposable {
        private readonly MeterListener _listener = new();

        public PresentationMetricListener() {
            _listener.InstrumentPublished = (instrument, listener) => {
                if (string.Equals(instrument.Meter.Name, PresentationApiTelemetry.TelemetryName, StringComparison.Ordinal)) {
                    listener.EnableMeasurementEvents(instrument);
                }
            };
            _listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, _) => {
                MetricMeasurement item = new(measurement, ToDictionary(tags));
                if (string.Equals(instrument.Name, "fooddiary.presentation.operations", StringComparison.Ordinal)) {
                    Operations.Add(item);
                } else if (string.Equals(instrument.Name, "fooddiary.presentation.operation.failures", StringComparison.Ordinal)) {
                    Failures.Add(item);
                }
            });
            _listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, _) => {
                if (string.Equals(instrument.Name, "fooddiary.presentation.operation.duration", StringComparison.Ordinal)) {
                    Durations.Add(new MetricMeasurement(Convert.ToInt64(measurement), ToDictionary(tags)));
                }
            });
            _listener.Start();
        }

        public List<MetricMeasurement> Operations { get; } = [];

        public List<MetricMeasurement> Durations { get; } = [];

        public List<MetricMeasurement> Failures { get; } = [];

        public void Dispose() => _listener.Dispose();

        private static IReadOnlyDictionary<string, object?> ToDictionary(ReadOnlySpan<KeyValuePair<string, object?>> tags) =>
            tags.ToArray().ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
    }

    [ExcludeFromCodeCoverage]
    private sealed class PresentationActivityListener : IDisposable {
        private readonly ActivityListener _listener;

        public PresentationActivityListener() {
            _listener = new ActivityListener {
                ShouldListenTo = source => string.Equals(source.Name, PresentationApiTelemetry.TelemetryName, StringComparison.Ordinal),
                Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                ActivityStopped = Completed.Add,
            };
            ActivitySource.AddActivityListener(_listener);
        }

        public List<Activity> Completed { get; } = [];

        public void Dispose() => _listener.Dispose();
    }

    [ExcludeFromCodeCoverage]
    private sealed record LogEntry(LogLevel Level);

    [ExcludeFromCodeCoverage]
    private sealed class RecordingLogger<T> : ILogger<T> {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) => Entries.Add(new LogEntry(logLevel));
    }
}
