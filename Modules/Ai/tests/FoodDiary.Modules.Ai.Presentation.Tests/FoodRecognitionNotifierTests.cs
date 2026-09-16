using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Modules.Ai.Presentation.Hubs;
using FoodDiary.Modules.Ai.Presentation.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace FoodDiary.Modules.Ai.Presentation.Tests;

[ExcludeFromCodeCoverage]
public sealed class FoodRecognitionNotifierTests {
    [Fact]
    public async Task DeliveryFailureIsRetriedOnNextPollAsync() {
        var update = new FoodRecognitionJobUpdate(Guid.NewGuid(), Guid.NewGuid());
        IFoodRecognitionJobReader reader = Substitute.For<IFoodRecognitionJobReader>();
        reader.GetUpdatesAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<FoodRecognitionJobUpdate>>([update]));
        var services = new ServiceCollection();
        services.AddSingleton(reader);
        await using ServiceProvider provider = services.BuildServiceProvider();
        IHubContext<FoodRecognitionHub> hub = Substitute.For<IHubContext<FoodRecognitionHub>>();
        IClientProxy client = Substitute.For<IClientProxy>();
        hub.Clients.User(update.UserId.ToString()).Returns(client);
        var delivered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int attempts = 0;
        client.SendCoreAsync("RecognitionChanged", Arg.Any<object?[]>(), Arg.Any<CancellationToken>()).Returns(call => {
            Assert.Equal(update.Id, Assert.Single(call.ArgAt<object?[]>(1)));
            if (Interlocked.Increment(ref attempts) == 1) { return Task.FromException(new InvalidOperationException("Connection lost")); }
            delivered.TrySetResult();
            return Task.CompletedTask;
        });
        using var notifier = new FoodRecognitionNotifier(provider.GetRequiredService<IServiceScopeFactory>(), hub,
            TimeProvider.System, NullLogger<FoodRecognitionNotifier>.Instance);
        await notifier.StartAsync(CancellationToken.None);
        try { await delivered.Task.WaitAsync(TimeSpan.FromSeconds(15)); } finally { await notifier.StopAsync(CancellationToken.None); }
        Assert.True(attempts >= 2);
    }

    [Fact]
    public async Task StopCancelsPendingReadAndCompletesGracefullyAsync() {
        IFoodRecognitionJobReader reader = Substitute.For<IFoodRecognitionJobReader>();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        reader.GetUpdatesAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(async call => {
            entered.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, call.Arg<CancellationToken>());
            return (IReadOnlyList<FoodRecognitionJobUpdate>)[];
        });
        var services = new ServiceCollection();
        services.AddSingleton(reader);
        await using ServiceProvider provider = services.BuildServiceProvider();
        using var notifier = new FoodRecognitionNotifier(provider.GetRequiredService<IServiceScopeFactory>(),
            Substitute.For<IHubContext<FoodRecognitionHub>>(), TimeProvider.System, NullLogger<FoodRecognitionNotifier>.Instance);
        await notifier.StartAsync(CancellationToken.None);
        try { await entered.Task.WaitAsync(TimeSpan.FromSeconds(15)); } finally { await notifier.StopAsync(CancellationToken.None); }
        Assert.NotNull(notifier.ExecuteTask);
        Assert.True(notifier.ExecuteTask.IsCompletedSuccessfully);
    }
}
