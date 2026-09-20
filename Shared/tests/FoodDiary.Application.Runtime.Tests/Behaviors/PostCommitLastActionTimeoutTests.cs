using FoodDiary.Application.Runtime.Common.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace FoodDiary.Application.Runtime.Tests.Behaviors;

[ExcludeFromCodeCoverage]
public sealed class PostCommitLastActionTimeoutTests {
    [Fact]
    public async Task LastActionExceedsFlushBudget_EmptiesQueueAndRestoresCapacity() {
        var time = new ManualTimerProvider();
        var queue = new PostCommitActionQueue(NullLogger<PostCommitActionQueue>.Instance, time,
            TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(10), maxActions: 1);
        queue.Enqueue("last", token => {
            time.Timers[0].Fire();
            token.ThrowIfCancellationRequested();
            throw new InvalidOperationException("Flush timeout must cancel the active action.");
        });

        await queue.FlushAsync();

        Assert.False(queue.HasActions);
        bool executed = false;
        queue.Enqueue("next", _ => { executed = true; return Task.CompletedTask; });
        await queue.FlushAsync();
        Assert.True(executed);
        Assert.False(queue.HasActions);
    }

    [ExcludeFromCodeCoverage]
    private sealed class ManualTimerProvider : TimeProvider {
        public List<ManualTimer> Timers { get; } = [];
        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period) {
            var timer = new ManualTimer(callback, state);
            Timers.Add(timer);
            return timer;
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class ManualTimer(TimerCallback callback, object? state) : ITimer {
        public void Fire() => callback(state);
        public bool Change(TimeSpan dueTime, TimeSpan period) => true;
        public void Dispose() { }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
