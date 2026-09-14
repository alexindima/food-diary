using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.JobManager.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace FoodDiary.JobManager.Tests;

[ExcludeFromCodeCoverage]
public sealed class FoodRecognitionWorkerTests {
    [Theory]
    [InlineData("processed")]
    [InlineData("idle")]
    [InlineData("error")]
    public async Task ExecuteAsync_ContinuesAfterWorkIdleOrFailureAndDisposesEveryScope(string firstOutcome) {
        var state = new ProcessorState(firstOutcome);
        await using ServiceProvider services = new ServiceCollection()
            .AddSingleton(state).AddScoped<IFoodRecognitionProcessor, Processor>().BuildServiceProvider();
        using var worker = new FoodRecognitionWorker(services.GetRequiredService<IServiceScopeFactory>(), NullLogger<FoodRecognitionWorker>.Instance);

        await worker.StartAsync(CancellationToken.None);
        await state.SecondCall.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await worker.StopAsync(CancellationToken.None);

        Assert.Equal(2, state.Calls);
        Assert.Equal(2, state.Disposed);
        Assert.True(worker.ExecuteTask!.IsCompletedSuccessfully);
    }

    [ExcludeFromCodeCoverage]
    private sealed class ProcessorState(string outcome) {
        public string Outcome { get; } = outcome;
        public TaskCompletionSource SecondCall { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int Calls;
        public int Disposed;
    }

    [ExcludeFromCodeCoverage]
    private sealed class Processor(ProcessorState state) : IFoodRecognitionProcessor, IDisposable {
        public async Task<bool> ProcessNextAsync(CancellationToken cancellationToken) {
            if (Interlocked.Increment(ref state.Calls) == 1) {
                return state.Outcome switch {
                    "processed" => true,
                    "idle" => false,
                    _ => throw new InvalidOperationException("Provider outcome is uncertain."),
                };
            }
            state.SecondCall.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return false;
        }

        public void Dispose() => Interlocked.Increment(ref state.Disposed);
    }
}
