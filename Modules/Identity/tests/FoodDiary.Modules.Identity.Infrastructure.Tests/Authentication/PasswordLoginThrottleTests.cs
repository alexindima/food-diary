using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Models;
using FoodDiary.Modules.Identity.Infrastructure.Authentication;
using Microsoft.Extensions.Options;

namespace FoodDiary.Modules.Identity.Infrastructure.Tests.Authentication;

[ExcludeFromCodeCoverage]
public sealed class PasswordLoginThrottleTests {
    [Fact]
    public async Task Failures_UseNormalizedEmail_AndBlockedRequestsDoNotExtendCooldownAsync() {
        var clock = new TestClock();
        InMemoryPasswordLoginThrottle throttle = Create(clock);
        await FailAsync(throttle, "USER@example.com");
        await FailAsync(throttle, " user@EXAMPLE.com ");
        await FailAsync(throttle, "user@example.com");

        PasswordLoginAttempt blocked = await throttle.TryBeginAsync("User@Example.com");
        Assert.False(blocked.IsAcquired);
        Assert.Equal(2, blocked.RetryAfterSeconds);
        clock.Advance(TimeSpan.FromSeconds(1));
        Assert.Equal(1, (await throttle.TryBeginAsync("user@example.com")).RetryAfterSeconds);
        Assert.True((await throttle.TryBeginAsync("different@example.com")).IsAcquired);
        clock.Advance(TimeSpan.FromSeconds(1));
        Assert.True((await throttle.TryBeginAsync("user@example.com")).IsAcquired);
        Assert.DoesNotContain("user", blocked.Key, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Cooldown_DoublesToCap_AndSuccessfulPasswordResetsFailuresAsync() {
        var clock = new TestClock();
        InMemoryPasswordLoginThrottle throttle = Create(clock);
        await FailAsync(throttle);
        await FailAsync(throttle);
        foreach (int seconds in new[] { 2, 4, 8, 8 }) {
            await FailAsync(throttle);
            Assert.Equal(seconds, (await throttle.TryBeginAsync("user@example.com")).RetryAfterSeconds);
            clock.Advance(TimeSpan.FromSeconds(seconds));
        }
        PasswordLoginAttempt success = await throttle.TryBeginAsync("user@example.com");
        Assert.True(await throttle.CompleteAsync(success, PasswordLoginAttemptOutcome.Succeeded));
        await FailAsync(throttle);
        await FailAsync(throttle);
        Assert.True((await throttle.TryBeginAsync("user@example.com")).IsAcquired);
    }

    [Fact]
    public async Task IdleFailureHistory_ExpiresAtBoundaryAsync() {
        var clock = new TestClock();
        InMemoryPasswordLoginThrottle throttle = Create(clock);
        await FailAsync(throttle);
        await FailAsync(throttle);
        clock.Advance(TimeSpan.FromSeconds(20));
        await FailAsync(throttle);
        Assert.True((await throttle.TryBeginAsync("user@example.com")).IsAcquired);
    }

    [Fact]
    public async Task ConcurrentAttempts_AdmitOneOwner_AndRejectStaleCompletionAsync() {
        var clock = new TestClock();
        InMemoryPasswordLoginThrottle throttle = Create(clock);
        PasswordLoginAttempt[] attempts = await Task.WhenAll(Enumerable.Range(0, 32).Select(_ =>
            Task.Run(() => throttle.TryBeginAsync("user@example.com"))));
        PasswordLoginAttempt original = Assert.Single(attempts, attempt => attempt.IsAcquired);
        clock.Advance(TimeSpan.FromSeconds(2));
        PasswordLoginAttempt successor = await throttle.TryBeginAsync("user@example.com");
        Assert.True(successor.IsAcquired);
        Assert.False(await throttle.CompleteAsync(original, PasswordLoginAttemptOutcome.Succeeded));
        Assert.False((await throttle.TryBeginAsync("user@example.com")).IsAcquired);
        Assert.True(await throttle.CompleteAsync(successor, PasswordLoginAttemptOutcome.InvalidCredentials));
        Assert.False(await throttle.CompleteAsync(successor, PasswordLoginAttemptOutcome.InvalidCredentials));
        Assert.True((await throttle.TryBeginAsync("user@example.com")).IsAcquired);
    }

    [Fact]
    public async Task AbandonedAttempts_DoNotCountAsFailuresAsync() {
        InMemoryPasswordLoginThrottle throttle = Create(new TestClock());
        for (int index = 0; index < 12; index++) {
            PasswordLoginAttempt attempt = await throttle.TryBeginAsync("user@example.com");
            Assert.True(attempt.IsAcquired);
            Assert.True(await throttle.CompleteAsync(attempt, PasswordLoginAttemptOutcome.Abandoned));
        }
        Assert.True((await throttle.TryBeginAsync("user@example.com")).IsAcquired);
    }

    [Fact]
    public async Task MemoryCapacity_DoesNotEvictActiveGuards_AndRecoversAfterExpiryAsync() {
        var clock = new TestClock();
        var throttle = new InMemoryPasswordLoginThrottle(Options.Create(new PasswordLoginThrottleOptions {
            MaximumTrackedEmails = 1,
        }), clock);
        PasswordLoginAttempt first = await throttle.TryBeginAsync("first@example.com");
        Assert.False((await throttle.TryBeginAsync("second@example.com")).IsAcquired);
        Assert.False((await throttle.TryBeginAsync("first@example.com")).IsAcquired);
        clock.Advance(TimeSpan.FromMinutes(1));
        Assert.True((await throttle.TryBeginAsync("second@example.com")).IsAcquired);
        Assert.False(await throttle.CompleteAsync(first, PasswordLoginAttemptOutcome.Succeeded));
    }

    [Fact]
    public async Task Cancellation_DoesNotMutateAttemptStateAsync() {
        InMemoryPasswordLoginThrottle throttle = Create(new TestClock());
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => throttle.TryBeginAsync("user@example.com", cancellation.Token));
        PasswordLoginAttempt attempt = await throttle.TryBeginAsync("user@example.com");
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            throttle.CompleteAsync(attempt, PasswordLoginAttemptOutcome.InvalidCredentials, cancellation.Token));
        Assert.True(await throttle.CompleteAsync(attempt, PasswordLoginAttemptOutcome.Abandoned));
    }

    [Fact]
    public void Options_RejectInvalidOrUnboundedSettings() {
        Assert.Multiple(
            () => Assert.True(PasswordLoginThrottleOptions.IsValid(new PasswordLoginThrottleOptions())),
            () => Assert.False(PasswordLoginThrottleOptions.IsValid(new PasswordLoginThrottleOptions { FailureThreshold = 0 })),
            () => Assert.False(PasswordLoginThrottleOptions.IsValid(new PasswordLoginThrottleOptions { MaximumCooldownSeconds = 901 })),
            () => Assert.False(PasswordLoginThrottleOptions.IsValid(new PasswordLoginThrottleOptions { AttemptLeaseSeconds = 0 })),
            () => Assert.False(PasswordLoginThrottleOptions.IsValid(new PasswordLoginThrottleOptions { MaximumTrackedEmails = 0 })));
    }

    private static InMemoryPasswordLoginThrottle Create(TestClock clock) => new(Options.Create(new PasswordLoginThrottleOptions {
        FailureThreshold = 3,
        FailureWindowSeconds = 20,
        InitialCooldownSeconds = 2,
        MaximumCooldownSeconds = 8,
        AttemptLeaseSeconds = 2,
    }), clock);

    private static async Task FailAsync(InMemoryPasswordLoginThrottle throttle, string email = "user@example.com") {
        PasswordLoginAttempt attempt = await throttle.TryBeginAsync(email);
        Assert.True(attempt.IsAcquired);
        Assert.True(await throttle.CompleteAsync(attempt, PasswordLoginAttemptOutcome.InvalidCredentials));
    }

    [ExcludeFromCodeCoverage]
    private sealed class TestClock : TimeProvider {
        private DateTimeOffset _now = new(2026, 10, 26, 12, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan duration) => _now += duration;
    }
}
