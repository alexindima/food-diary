using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;
using System.Reflection.Emit;
using FoodDiary.Presentation.Api.Extensions;
using FoodDiary.Presentation.Api.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace FoodDiary.Presentation.Api.Tests;

[Collection(PresentationTelemetryCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class TelemetryActionFilterTests {
    [Fact]
    public async Task OnActionExecutionAsync_WithSuccessfulAction_RecordsOneCompletedOperation() {
        using var metrics = new PresentationMetricListener();
        using var activities = new PresentationActivityListener();
        var logger = new RecordingLogger<TelemetryActionFilter>();
        var filter = new TelemetryActionFilter(logger);
        ActionExecutingContext context = CreateActionExecutingContext(
            new TelemetryProbeController(),
            actionName: "Get",
            statusCode: StatusCodes.Status200OK);
        bool nextCalled = false;

        await filter.OnActionExecutionAsync(context, () => {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(context, [], context.Controller));
        });

        MetricMeasurement operation = Assert.Single(metrics.Operations);
        MetricMeasurement duration = Assert.Single(metrics.Durations);
        Activity activity = Assert.Single(activities.Completed);
        Assert.Multiple(
            () => Assert.True(nextCalled),
            () => Assert.Empty(metrics.Failures),
            () => Assert.Empty(logger.Entries),
            () => Assert.Equal(1, operation.Value),
            () => Assert.Equal("Unknown", operation.Tags["fooddiary.presentation.feature"]),
            () => Assert.Equal("TelemetryProbeController.Get", operation.Tags["fooddiary.presentation.operation"]),
            () => Assert.Equal("success", operation.Tags["fooddiary.presentation.outcome"]),
            () => Assert.True(duration.Value >= 0),
            () => Assert.Equal("success", activity.GetTagItem("fooddiary.presentation.outcome")),
            () => Assert.Equal(StatusCodes.Status200OK, activity.GetTagItem("http.response.status_code")));
    }

    [Fact]
    public async Task OnActionExecutionAsync_WithClientFailure_RecordsFailureAndLogsInformation() {
        using var metrics = new PresentationMetricListener();
        using var activities = new PresentationActivityListener();
        var logger = new RecordingLogger<TelemetryActionFilter>();
        var filter = new TelemetryActionFilter(logger);
        ActionExecutingContext context = CreateActionExecutingContext(
            controller: new object(),
            actionName: null,
            statusCode: StatusCodes.Status400BadRequest);

        await filter.OnActionExecutionAsync(context, () => Task.FromResult(
            new ActionExecutedContext(context, [], context.Controller)));

        MetricMeasurement operation = Assert.Single(metrics.Operations);
        MetricMeasurement failure = Assert.Single(metrics.Failures);
        LogEntry log = Assert.Single(logger.Entries);
        Activity activity = Assert.Single(activities.Completed);
        Assert.Multiple(
            () => Assert.Equal("failure", operation.Tags["fooddiary.presentation.outcome"]),
            () => Assert.Equal("HttpStatus_400", failure.Tags["error.code"]),
            () => Assert.Equal("Unknown", failure.Tags["fooddiary.presentation.feature"]),
            () => Assert.Equal(LogLevel.Information, log.Level),
            () => Assert.Equal(ActivityStatusCode.Unset, activity.Status));
    }

    [Fact]
    public async Task OnActionExecutionAsync_WithServerFailure_RecordsFailureAndMarksActivityAsError() {
        using var metrics = new PresentationMetricListener();
        using var activities = new PresentationActivityListener();
        var logger = new RecordingLogger<TelemetryActionFilter>();
        var filter = new TelemetryActionFilter(logger);
        ActionExecutingContext context = CreateActionExecutingContext(
            new TelemetryProbeController(),
            actionName: "Post",
            statusCode: StatusCodes.Status503ServiceUnavailable);

        await filter.OnActionExecutionAsync(context, () => Task.FromResult(
            new ActionExecutedContext(context, [], context.Controller)));

        MetricMeasurement failure = Assert.Single(metrics.Failures);
        Activity activity = Assert.Single(activities.Completed);
        LogEntry log = Assert.Single(logger.Entries);
        Assert.Multiple(
            () => Assert.Equal("HttpStatus_503", failure.Tags["error.code"]),
            () => Assert.Equal(ActivityStatusCode.Error, activity.Status),
            () => Assert.Equal(LogLevel.Warning, log.Level));
    }

    [Fact]
    public async Task OnActionExecutionAsync_WithActionException_RecordsUnhandledFailure() {
        using var metrics = new PresentationMetricListener();
        using var activities = new PresentationActivityListener();
        var logger = new RecordingLogger<TelemetryActionFilter>();
        var filter = new TelemetryActionFilter(logger);
        ActionExecutingContext context = CreateActionExecutingContext(
            new TelemetryProbeController(),
            actionName: "Post",
            statusCode: StatusCodes.Status200OK);
        var exception = new InvalidOperationException("boom");

        await filter.OnActionExecutionAsync(context, () => Task.FromResult(
            new ActionExecutedContext(context, [], context.Controller) {
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

    private static ActionExecutingContext CreateActionExecutingContext(
        object controller,
        string? actionName,
        int statusCode) {
        var httpContext = new DefaultHttpContext();
        httpContext.Response.StatusCode = statusCode;

        var routeValues = new Dictionary<string, string?>(StringComparer.Ordinal);
        if (actionName is not null) {
            routeValues["action"] = actionName;
        }

        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor {
                RouteValues = routeValues,
            });

        return new ActionExecutingContext(
            actionContext,
            [],
            new Dictionary<string, object?>(StringComparer.Ordinal),
            controller);
    }

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
