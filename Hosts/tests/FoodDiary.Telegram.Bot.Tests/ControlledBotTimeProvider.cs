using System.Threading.Channels;

namespace FoodDiary.Telegram.Bot.Tests;

[ExcludeFromCodeCoverage]
internal sealed class ControlledBotTimeProvider : TimeProvider {
    private readonly Channel<ScheduledDelay> _delays = Channel.CreateUnbounded<ScheduledDelay>();

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period) {
        var timer = new ScheduledDelay(callback, state, dueTime);
        _delays.Writer.TryWrite(timer);
        return timer;
    }

    public Task<ScheduledDelay> NextDelayAsync() => _delays.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);

    [ExcludeFromCodeCoverage]
    internal sealed class ScheduledDelay(TimerCallback callback, object? state, TimeSpan dueTime) : ITimer {
        public TimeSpan DueTime { get; } = dueTime;
        public void Fire() => callback(state);
        public bool Change(TimeSpan dueTime, TimeSpan period) => throw new NotSupportedException();
        public void Dispose() { }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
