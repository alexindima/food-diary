using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Recipes.Application.Abstractions.Moderation;
using FoodDiary.Modules.Recipes.Domain.Entities;
using FoodDiary.Modules.Recipes.Infrastructure.Moderation;
using FoodDiary.Modules.Users.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

public sealed partial class RecipeModerationIntegrationTests {
    [RequiresDockerTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ModerationWatchdogRetriesWhileCallerCancellationPreservesTheLeaseAsync(bool callerCancels) {
        await using FoodDiaryDbContext database = await databaseFixture.CreateDbContextAsync();
        User author = await CreateModerationAuthorAsync(database);
        IRecipeModerationProvider openAi = CreateProviderStub("OpenAi");
        var startedAt = new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero);
        DateTimeOffset now = startedAt;
        var watchdogDelay = TimeSpan.FromMinutes(4.5);
        TimeProvider clock = Substitute.For<TimeProvider>();
        clock.GetUtcNow().Returns(_ => now);
        ITimer timer = Substitute.For<ITimer>();
        Action? fireWatchdog = null;
        clock.CreateTimer(Arg.Any<TimerCallback>(), Arg.Any<object?>(), watchdogDelay, Timeout.InfiniteTimeSpan)
            .Returns(call => {
                TimerCallback callback = call.Arg<TimerCallback>();
                object? state = call[1];
                fireWatchdog = () => callback(state);
                return timer;
            });
        await using ServiceProvider provider = CreateProvider(database.Database.GetConnectionString()!, openAi, clock: clock);
        Guid revisionId = GetRevisionId(await CreatePendingAsync(provider, author.Id));
        using var caller = new CancellationTokenSource();
        openAi.CheckAsync(Arg.Any<RecipeModerationContent>(), Arg.Any<CancellationToken>()).Returns(async call => {
            if (callerCancels) {
                await caller.CancelAsync();
            } else {
                Assert.NotNull(fireWatchdog);
                now += watchdogDelay;
                fireWatchdog();
            }

            return await Task.FromCanceled<RecipeModerationCheck>(call.Arg<CancellationToken>());
        });

        if (callerCancels) {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ProcessAsync(provider, cancellationToken: caller.Token));
        } else {
            Assert.True(await ProcessAsync(provider, cancellationToken: caller.Token));
        }

        RecipePublicationRevision pending = await ReadRevisionAsync(database, revisionId);
        Assert.Multiple(
            () => Assert.Equal(RecipePublicationStatuses.Pending, pending.Status),
            () => Assert.Equal(1, pending.Attempts),
            () => Assert.Null(pending.PrimaryResult));
        if (callerCancels) {
            Assert.Multiple(
                () => Assert.NotNull(pending.LeaseToken),
                () => Assert.Equal(startedAt.AddMinutes(5), pending.LeaseUntil),
                () => Assert.Equal(startedAt, pending.NextAttemptAt));
        } else {
            DateTimeOffset nextAttempt = Assert.IsType<DateTimeOffset>(pending.NextAttemptAt);
            Assert.Multiple(
                () => Assert.Null(pending.LeaseToken),
                () => Assert.Null(pending.LeaseUntil),
                () => Assert.InRange(nextAttempt, now.AddSeconds(10), now.AddSeconds(14)));
        }
        clock.Received(1).CreateTimer(Arg.Any<TimerCallback>(), Arg.Any<object?>(), watchdogDelay, Timeout.InfiniteTimeSpan);
        timer.Received(1).Dispose();
    }
}
