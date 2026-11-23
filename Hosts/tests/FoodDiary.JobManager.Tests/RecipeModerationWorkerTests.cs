using FoodDiary.JobManager.Services;
using FoodDiary.Modules.Recipes.Infrastructure.Moderation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.Core;

namespace FoodDiary.JobManager.Tests;

[ExcludeFromCodeCoverage]
public sealed class RecipeModerationWorkerTests {
    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(15);

    [Fact]
    public async Task ExecuteAsync_ContinuesBothLanesAfterIdleOrFailureAndCancelsInFlightWork() {
        var scenario = new Scenario(firstIterationHasWork: false);
        ILogger<RecipeModerationWorker> logger = Substitute.For<ILogger<RecipeModerationWorker>>();
        await using ServiceProvider services = CreateServices(scenario);
        using var worker = new RecipeModerationWorker(services.GetRequiredService<IServiceScopeFactory>(), logger);

        await RunUntilReadyAsync(worker, scenario);

        AssertStoppedAndDisposed(worker, scenario);
        ICall log = Assert.Single(logger.ReceivedCalls(), call => string.Equals(call.GetMethodInfo().Name, nameof(ILogger.Log), StringComparison.Ordinal));
        object?[] arguments = log.GetArguments();
        var fields = Assert.IsAssignableFrom<IEnumerable<KeyValuePair<string, object?>>>(arguments[2]).ToDictionary(StringComparer.Ordinal);
        Assert.Multiple(
            () => Assert.Equal(LogLevel.Error, arguments[0]),
            () => Assert.Equal(false, fields["Observation"]),
            () => Assert.Equal(nameof(InvalidOperationException), fields["ExceptionType"]),
            () => Assert.Null(arguments[3]));
    }

    [Fact]
    public async Task ExecuteAsync_DrainsBothLanesAndStopsDuringIdleWait() {
        var scenario = new Scenario(firstIterationHasWork: true);
        ILogger<RecipeModerationWorker> logger = Substitute.For<ILogger<RecipeModerationWorker>>();
        await using ServiceProvider services = CreateServices(scenario);
        using var worker = new RecipeModerationWorker(services.GetRequiredService<IServiceScopeFactory>(), logger);

        await RunUntilReadyAsync(worker, scenario);

        AssertStoppedAndDisposed(worker, scenario);
        Assert.Empty(logger.ReceivedCalls());
    }

    private static ServiceProvider CreateServices(Scenario scenario) =>
        new ServiceCollection()
            .AddSingleton(scenario)
            .AddScoped<IRecipeModerationProcessor, Processor>()
            .BuildServiceProvider();

    private static async Task RunUntilReadyAsync(RecipeModerationWorker worker, Scenario scenario) {
        await worker.StartAsync(CancellationToken.None);
        try {
            await Task.WhenAll(scenario.Primary.Ready.Task, scenario.Observation.Ready.Task).WaitAsync(TestTimeout);
        } finally {
            using var stopTimeout = new CancellationTokenSource(TestTimeout);
            await worker.StopAsync(stopTimeout.Token);
        }
    }

    private static void AssertStoppedAndDisposed(RecipeModerationWorker worker, Scenario scenario) =>
        Assert.Multiple(
            () => Assert.True(worker.ExecuteTask?.IsCompletedSuccessfully),
            () => Assert.Equal(2, scenario.Primary.Calls),
            () => Assert.Equal(2, scenario.Primary.Disposals),
            () => Assert.Equal(2, scenario.Observation.Calls),
            () => Assert.Equal(2, scenario.Observation.Disposals));

    [ExcludeFromCodeCoverage]
    private sealed class Scenario(bool firstIterationHasWork) {
        public bool FirstIterationHasWork { get; } = firstIterationHasWork;
        public Lane Primary { get; } = new();
        public Lane Observation { get; } = new();
    }

    [ExcludeFromCodeCoverage]
    private sealed class Lane {
        public TaskCompletionSource Ready { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int Calls;
        public int Disposals;
    }

    [ExcludeFromCodeCoverage]
    private sealed class Processor(Scenario scenario) : IRecipeModerationProcessor, IAsyncDisposable {
        private Lane? _lane;

        public Task<bool> ProcessNextAsync(bool observation, CancellationToken cancellationToken) {
            Lane lane = observation ? scenario.Observation : scenario.Primary;
            _lane = lane;
            if (Interlocked.Increment(ref lane.Calls) == 1) {
                if (!scenario.FirstIterationHasWork && !observation) {
                    return Task.FromException<bool>(new InvalidOperationException("Provider details."));
                }

                return Task.FromResult(scenario.FirstIterationHasWork);
            }

            if (scenario.FirstIterationHasWork) {
                return Task.FromResult(false);
            }

            lane.Ready.TrySetResult();
            return WaitForShutdownAsync(cancellationToken);
        }

        public ValueTask DisposeAsync() {
            if (_lane is { } lane && Interlocked.Increment(ref lane.Disposals) == 2 && scenario.FirstIterationHasWork) {
                lane.Ready.TrySetResult();
            }

            return ValueTask.CompletedTask;
        }

        private static async Task<bool> WaitForShutdownAsync(CancellationToken cancellationToken) {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return false;
        }
    }
}
