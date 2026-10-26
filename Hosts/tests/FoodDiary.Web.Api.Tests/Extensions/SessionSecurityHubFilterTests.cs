using System.Security.Claims;
using FoodDiary.Modules.Identity.Contracts.Authentication.Common;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Web.Api.Extensions.Filters;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace FoodDiary.Web.Api.Tests.Extensions;

[ExcludeFromCodeCoverage]
public sealed class SessionSecurityHubFilterTests {
    [Theory]
    [InlineData("revoked")]
    [InlineData("security-changed")]
    [InlineData("store-unavailable")]
    [InlineData("timeout")]
    [InlineData("healthy")]
    public async Task Monitor_AbortsOnRejectedOrUnverifiableState_WithoutClientInvocations(string scenario) {
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        IUserAccessTokenSecurityReader security = Substitute.For<IUserAccessTokenSecurityReader>();
        IUserAccessTokenSessionReader sessions = Substitute.For<IUserAccessTokenSessionReader>();
        var checkedSession = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        security.IsCurrentAsync(userId, 7, Arg.Any<CancellationToken>()).Returns(!string.Equals(scenario, "security-changed", StringComparison.Ordinal));
        sessions.IsActiveAsync(userId, sessionId, Arg.Any<CancellationToken>()).Returns(_ => {
            checkedSession.TrySetResult();
            return !string.Equals(scenario, "revoked", StringComparison.Ordinal);
        });
        if (string.Equals(scenario, "store-unavailable", StringComparison.Ordinal)) {
            security.IsCurrentAsync(userId, 7, Arg.Any<CancellationToken>()).Returns(Task.FromException<bool>(new InvalidOperationException("unavailable")));
        } else if (string.Equals(scenario, "timeout", StringComparison.Ordinal)) {
            security.IsCurrentAsync(userId, 7, Arg.Any<CancellationToken>()).Returns(async call => {
                await Task.Delay(Timeout.InfiniteTimeSpan, call.ArgAt<CancellationToken>(2));
                return true;
            });
        }
        await using ServiceProvider provider = new ServiceCollection().AddSingleton(security).AddSingleton(sessions).BuildServiceProvider();
        var clock = new ManualClock();
        using var connection = new CancellationTokenSource();
        var aborted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        HubCallerContext caller = Substitute.For<HubCallerContext>();
        caller.User.Returns(new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, userId.ToString("D")),
            new Claim(JwtSecurityClaimNames.SecurityVersion, "7"),
            new Claim(JwtSecurityClaimNames.RefreshSessionId, sessionId.ToString("D")),
        ], "test")));
        caller.ConnectionAborted.Returns(connection.Token);
        caller.Items.Returns(new Dictionary<object, object?>());
        caller.When(value => value.Abort()).Do(_ => aborted.TrySetResult());
        var lifetime = new HubLifetimeContext(caller, provider, new TestHub());
        var filter = new SessionSecurityHubFilter(provider.GetRequiredService<IServiceScopeFactory>(), clock, NullLogger<SessionSecurityHubFilter>.Instance);
        await filter.OnConnectedAsync(lifetime, _ => Task.CompletedTask);
        Assert.False(aborted.Task.IsCompleted);
        clock.Tick(TimeSpan.FromSeconds(15));
        if (string.Equals(scenario, "timeout", StringComparison.Ordinal)) {
            await clock.TimeoutCreated.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
            clock.Tick(TimeSpan.FromSeconds(5));
        }
        if (string.Equals(scenario, "healthy", StringComparison.Ordinal)) {
            await checkedSession.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
            Assert.False(aborted.Task.IsCompleted);
        } else {
            await aborted.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        }
        await filter.OnDisconnectedAsync(lifetime, exception: null, (_, _) => Task.CompletedTask);
        Assert.Empty(caller.Items);
        Assert.All(clock.Timers, timer => Assert.True(timer.Disposed));
    }

    [ExcludeFromCodeCoverage]
    private sealed class TestHub : Hub;

    [ExcludeFromCodeCoverage]
    private sealed class ManualClock : TimeProvider {
        public List<ManualTimer> Timers { get; } = [];
        public TaskCompletionSource TimeoutCreated { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period) {
            var timer = new ManualTimer(callback, state, dueTime);
            Timers.Add(timer);
            if (dueTime == TimeSpan.FromSeconds(5)) {
                TimeoutCreated.TrySetResult();
            }
            return timer;
        }
        public void Tick(TimeSpan dueTime) {
            foreach (ManualTimer timer in Timers.ToArray().Where(timer => !timer.Disposed && timer.DueTime == dueTime)) {
                timer.Callback(timer.State);
            }
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class ManualTimer(TimerCallback callback, object? state, TimeSpan dueTime) : ITimer {
        public TimerCallback Callback { get; } = callback;
        public object? State { get; } = state;
        public TimeSpan DueTime { get; } = dueTime;
        public bool Disposed { get; private set; }
        public bool Change(TimeSpan dueTime, TimeSpan period) => !Disposed;
        public void Dispose() => Disposed = true;
        public ValueTask DisposeAsync() { Disposed = true; return ValueTask.CompletedTask; }
    }
}
