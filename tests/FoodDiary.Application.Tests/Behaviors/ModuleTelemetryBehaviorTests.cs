using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using FoodDiary.Application.Runtime.Common.Behaviors;
using FoodDiary.Application.Runtime.Common.Services;
using FoodDiary.Mediator;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests.Behaviors;

[ExcludeFromCodeCoverage]
public sealed class ModuleTelemetryBehaviorTests {
    [Fact]
    public async Task Handle_ConcurrentCallsRemainActiveUntilBothComplete() {
        var measurements = new ConcurrentQueue<Measurement>();
        using MeterListener listener = Listen(measurements);
        var behavior = new ModuleTelemetryBehavior<TelemetryTestRequest, Result>();
        var release = new TaskCompletionSource<Result>(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<Result> first = behavior.Handle(new TelemetryTestRequest("first"), _ => release.Task, CancellationToken.None);
        Task<Result> second = behavior.Handle(new TelemetryTestRequest("second"), _ => release.Task, CancellationToken.None);

        Assert.Equal(2, measurements.Where(x => x.Name is "fooddiary.module.active").Sum(x => x.Value));
        release.SetResult(Result.Success());
        await Task.WhenAll(first, second).WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Multiple(
            () => Assert.Equal(0, measurements.Where(x => x.Name is "fooddiary.module.active").Sum(x => x.Value)),
            () => Assert.Equal(2, measurements.Where(x => x.Name is "fooddiary.module.operations").Sum(x => x.Value)));
    }

    [Fact]
    public void ResolveModule_RecognizesAnActualModuleRequestAssembly() =>
        Assert.Equal("Products", ModuleOperationTelemetry.ResolveModule(
            typeof(FoodDiary.Application.Products.Queries.GetProducts.GetProductsQuery).Assembly.GetName().Name));

    [Theory]
    [InlineData("success")]
    [InlineData("failure")]
    [InlineData("exception")]
    [InlineData("cancelled")]
    [InlineData("unrequested_cancellation")]
    public async Task Handle_RecordsOutcomeAndBalancesActiveOperationsWithoutPayload(string scenario) {
        var measurements = new ConcurrentQueue<Measurement>();
        using MeterListener listener = Listen(measurements);
        using var cancellation = new CancellationTokenSource();
        var behavior = new ModuleTelemetryBehavior<TelemetryTestRequest, Result>();
        var request = new TelemetryTestRequest("private-payload-marker");
        var expectedException = new InvalidOperationException("private-exception-marker");
        Result expectedResult = scenario is "failure"
            ? Result.Failure(new Error("Private.Error", "private-error-marker"))
            : Result.Success();

        async Task<Result> ExecuteAsync() => await behavior.Handle(request, async token => {
            Assert.Equal(cancellation.Token, token);
            Assert.Equal(1, measurements.Where(x => x.Name is "fooddiary.module.active").Sum(x => x.Value));
            if (scenario is "exception") {
                throw expectedException;
            }

            if (scenario is "cancelled" or "unrequested_cancellation") {
                if (scenario is "cancelled") {
                    await cancellation.CancelAsync();
                }

                throw new OperationCanceledException(token);
            }

            return expectedResult;
        }, cancellation.Token);

        if (scenario is "exception") {
            Assert.Same(expectedException, await Assert.ThrowsAsync<InvalidOperationException>(ExecuteAsync));
        } else if (scenario is "cancelled" or "unrequested_cancellation") {
            await Assert.ThrowsAsync<OperationCanceledException>(ExecuteAsync);
        } else {
            Assert.Same(expectedResult, await ExecuteAsync());
        }

        Measurement completed = Assert.Single(measurements, x => x.Name is "fooddiary.module.operations");
        Measurement duration = Assert.Single(measurements, x => x.Name is "fooddiary.module.operation.duration");
        Assert.Multiple(
            () => Assert.Equal(scenario is "unrequested_cancellation" ? "exception" : scenario, completed.Tags["outcome"]),
            () => Assert.Equal(1, completed.Value),
            () => Assert.True(duration.Value >= 0),
            () => Assert.Equal(0, measurements.Where(x => x.Name is "fooddiary.module.active").Sum(x => x.Value)),
            () => Assert.Equal(2, measurements.Count(x => x.Name is "fooddiary.module.active")),
            () => Assert.Equal(3, completed.Tags.Count),
            () => Assert.Equal(2, duration.Tags.Count));
        Assert.All(measurements, measurement => {
            Assert.Equal("Other", measurement.Tags["module"]);
            Assert.Equal(nameof(TelemetryTestRequest), measurement.Tags["operation"]);
            Assert.DoesNotContain(measurement.Tags.Values, value => value.Contains("private", StringComparison.Ordinal));
        });
    }

    [Theory]
    [InlineData("FoodDiary.Application.Meals", "Meals")]
    [InlineData("FoodDiary.Application.BodyMetrics", "BodyMetrics")]
    [InlineData("FoodDiary.Application.Tests", "Other")]
    [InlineData("FoodDiary.Application.Contracts", "Other")]
    [InlineData("FoodDiary.Application.", "Other")]
    [InlineData("Unrelated.Assembly", "Other")]
    [InlineData(null, "Other")]
    public void ResolveModule_UsesOnlyApplicationAssemblyOwnership(string? assembly, string expected) =>
        Assert.Equal(expected, ModuleOperationTelemetry.ResolveModule(assembly));

    private static MeterListener Listen(ConcurrentQueue<Measurement> measurements) {
        var listener = new MeterListener {
            InstrumentPublished = (instrument, meterListener) => {
                if (instrument.Meter.Name is "FoodDiary.Application.Runtime" && instrument.Name.StartsWith("fooddiary.module.", StringComparison.Ordinal)) {
                    meterListener.EnableMeasurementEvents(instrument);
                }
            },
        };
        listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) => Record(instrument, value, tags));
        listener.SetMeasurementEventCallback<double>((instrument, value, tags, _) => Record(instrument, value, tags));
        listener.Start();
        return listener;

        void Record(Instrument instrument, double value, ReadOnlySpan<KeyValuePair<string, object?>> tags) {
            Dictionary<string, string> values = tags.ToArray().ToDictionary(x => x.Key, x => x.Value?.ToString() ?? string.Empty, StringComparer.Ordinal);
            if (string.Equals(values.GetValueOrDefault("operation"), nameof(TelemetryTestRequest), StringComparison.Ordinal)) {
                measurements.Enqueue(new Measurement(instrument.Name, value, values));
            }
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed record TelemetryTestRequest(string Payload) : IRequest<Result>;

    [ExcludeFromCodeCoverage]
    private sealed record Measurement(string Name, double Value, Dictionary<string, string> Tags);
}
