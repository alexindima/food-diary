using System.Data.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Outbox;
using FoodDiary.Application.Abstractions.Email.Common;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Achievements;
using FoodDiary.Infrastructure.Persistence.Email;
using FoodDiary.Infrastructure.Persistence.Images;
using FoodDiary.Infrastructure.Persistence.Notifications;
using FoodDiary.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class OutboxReplayBoundaryIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    private static readonly DateTime Now = new(2026, 9, 3, 10, 0, 0, DateTimeKind.Utc);
    private static readonly TimeProvider Clock = new ReplayTimeProvider();

    [RequiresDockerFact]
    public async Task Preview_LooksUpOnlyRequestedIdsAcrossAllStreams() {
        await using FoodDiaryDbContext seed = await databaseFixture.CreateDbContextAsync();
        await CreateLookupBatchAsync(seed, "other", deadLettered: true);
        await using FoodDiaryDbContext observer = databaseFixture.CreateDbContext(seed.Database.GetConnectionString()!);
        using var scope = new OutboxReplayTestScope(observer, Clock);
        string[] names = ["email", "image_object_deletion", "notification_web_push", "achievement_evaluation"];
        foreach (string name in names) {
            Assert.Null(await scope.Service.GetDeadLetterAsync(name, Guid.NewGuid()));
            Assert.Empty(observer.ChangeTracker.Entries());
        }

        IOutboxMessage[] requested = await CreateLookupBatchAsync(seed, "requested", deadLettered: true);
        IOutboxMessage[] active = await CreateLookupBatchAsync(seed, "active", deadLettered: false);
        for (int index = 0; index < names.Length; index++) {
            observer.ChangeTracker.Clear();
            OutboxDeadLetterMessageModel? preview = await scope.Service.GetDeadLetterAsync(names[index], requested[index].Id);
            Assert.NotNull(preview);
            Assert.Multiple(
                () => Assert.Equal(requested[index].Id, preview.MessageId),
                () => Assert.Equal("failure-requested", preview.LastError),
                () => Assert.Equal(1, preview.AttemptCount),
                () => Assert.Equal(Now, preview.DeadLetteredOnUtc));
            EntityEntry tracked = Assert.Single(observer.ChangeTracker.Entries());
            Assert.Equal(requested[index].Id, Assert.IsAssignableFrom<IOutboxMessage>(tracked.Entity).Id);
            Assert.Equal(EntityState.Unchanged, tracked.State);
            observer.ChangeTracker.Clear();
            Assert.Null(await scope.Service.GetDeadLetterAsync(names[index], active[index].Id));
            observer.ChangeTracker.Clear();
            Assert.Null(await scope.Service.GetDeadLetterAsync(names[index], Guid.NewGuid()));
            Assert.Empty(observer.ChangeTracker.Entries());
            Assert.Equal(2, (await scope.Service.ListDeadLettersAsync(names[index], 10)).Count);
        }
        Assert.Empty(await observer.OutboxReplayAudits.ToListAsync());
    }

    [RequiresDockerFact]
    public async Task Replay_PreservesFourStreamOrderingMetadataAndAtomicAudit() {
        await using FoodDiaryDbContext seed = await databaseFixture.CreateDbContextAsync();
        var user = User.Create("replay-boundary@example.com", "hash");
        var notification = Notification.Create(user.Id, "info", "{}");
        var email = EmailOutboxMessage.Create(new EmailMessage("from@example.com", "Sender", ["to@example.com"], "Private", "body", TextBody: null), Now.AddMinutes(-2));
        var image = ImageObjectDeletionOutboxMessage.Create("users/test/dead.webp", Now.AddMinutes(-2));
        var push = NotificationWebPushOutboxMessage.Create(notification.Id, Now.AddMinutes(-2));
        var achievement = AchievementEvaluationOutboxMessage.Create(user.Id, Now.AddMinutes(-2));
        IOutboxMessage[] messages = [email, image, push, achievement];
        foreach (IOutboxMessage message in messages) {
            message.MarkDeadLettered("previous error", Now.AddMinutes(-1));
        }
        seed.AddRange(user, notification, email, image, push, achievement);
        await seed.SaveChangesAsync();
        var capture = new LockCommandCapture();
        await using FoodDiaryDbContext context = CreateContext(seed.Database.GetConnectionString()!, capture);
        using var scope = new OutboxReplayTestScope(context, Clock);
        IOutboxDeadLetterReplayService service = scope.Service;
        string[] names = ["email", "image_object_deletion", "notification_web_push", "achievement_evaluation"];
        string[] summaries = [string.Empty, image.ObjectKey, push.NotificationId.Value.ToString(), achievement.UserId.Value.ToString()];
        IReadOnlyList<OutboxDeadLetterMessageModel> all = await service.ListDeadLettersAsync(outboxName: null, 200);
        Assert.Equal(names, all.Select(item => item.OutboxName), StringComparer.Ordinal);
        Assert.Equal(names.Take(2), (await service.ListDeadLettersAsync(" ", 2)).Select(item => item.OutboxName), StringComparer.Ordinal);
        Assert.Empty(context.ChangeTracker.Entries());
        for (int index = 0; index < names.Length; index++) {
            OutboxDeadLetterMessageModel? preview = await service.GetDeadLetterAsync($" {names[index].ToUpperInvariant()} ", messages[index].Id);
            Assert.NotNull(preview);
            Assert.Equal(summaries[index], preview.Summary);
        }
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ReplayAsync("email", email.Id, "operator", "reason", 1));
        Assert.Empty(capture.Commands);
        for (int index = 1; index < names.Length; index++) {
            OutboxReplayAuditModel audit = await service.ReplayAsync(names[index], messages[index].Id, " operator ", " recovered ", 1);
            Assert.Multiple(
                () => Assert.Equal(names[index], audit.OutboxName),
                () => Assert.Equal(messages[index].Id, audit.MessageId),
                () => Assert.Equal("operator", audit.RequestedBy),
                () => Assert.Equal("recovered", audit.Reason),
                () => Assert.Equal("previous error", audit.PreviousError),
                () => Assert.Equal(1, audit.PreviousAttemptCount),
                () => Assert.Equal(Now, audit.RequestedOnUtc),
                () => Assert.Null(context.Database.CurrentTransaction));
        }
        Assert.Equal(3, capture.Commands.Count);
        Assert.All(capture.Commands, command => Assert.True(command.HasTransaction));
        Assert.Contains(capture.Commands, command => command.Sql.Contains("ImageObjectDeletionOutbox", StringComparison.Ordinal));
        Assert.Contains(capture.Commands, command => command.Sql.Contains("NotificationWebPushOutbox", StringComparison.Ordinal));
        Assert.Contains(capture.Commands, command => command.Sql.Contains("AchievementEvaluationOutbox", StringComparison.Ordinal));
        context.ChangeTracker.Clear();
        Assert.Equal(3, await context.OutboxReplayAudits.CountAsync());
        Assert.Single(await service.ListReplayHistoryAsync(" notification_web_push ", push.Id, 10));
        using var persisted = new OutboxReplayTestScope(context, Clock);
        foreach (IOutboxReplayStream stream in persisted.Streams.Where(stream => !string.Equals(stream.Name, "email", StringComparison.Ordinal))) {
            OutboxReplayEntry? entry = await stream.FindAsync(messages[Array.IndexOf(names, stream.Name)].Id, forUpdate: false);
            Assert.NotNull(entry);
            Assert.Multiple(
                () => Assert.Null(entry.Message.DeadLetteredOnUtc),
                () => Assert.Null(entry.Message.ProcessedOnUtc),
                () => Assert.Null(entry.Message.LockedUntilUtc),
                () => Assert.Null(entry.Message.LockedBy),
                () => Assert.Equal(1, entry.Message.AttemptCount),
                () => Assert.Null(entry.LastError));
        }
        Assert.Equal(achievement.Revision, (await context.AchievementEvaluationOutbox.SingleAsync()).Revision);
        EmailOutboxMessage storedEmail = await context.EmailOutbox.SingleAsync();
        Assert.Equal("[]", storedEmail.ToAddressesJson);
        Assert.Empty(storedEmail.HtmlBody);
        Assert.NotNull(storedEmail.DeadLetteredOnUtc);
    }

    [RequiresDockerFact]
    public async Task SaveFailure_RollsBackReplayAndAudit_AndReleasesTransaction() {
        await using FoodDiaryDbContext seed = await databaseFixture.CreateDbContextAsync();
        ImageObjectDeletionOutboxMessage image = await SeedImageAsync(seed);
        string connection = seed.Database.GetConnectionString()!;
        await using (FoodDiaryDbContext failing = CreateContext(connection, new FailingSaveInterceptor())) {
            using var scope = new OutboxReplayTestScope(failing, Clock);
            await Assert.ThrowsAsync<InvalidOperationException>(() => scope.Service.ReplayAsync("image_object_deletion", image.Id, "operator", "retry", 1));
            Assert.Null(failing.Database.CurrentTransaction);
        }
        await using FoodDiaryDbContext observer = databaseFixture.CreateDbContext(connection);
        Assert.NotNull((await observer.ImageObjectDeletionOutbox.SingleAsync()).DeadLetteredOnUtc);
        Assert.Empty(await observer.OutboxReplayAudits.ToListAsync());
        using var retry = new OutboxReplayTestScope(observer, Clock);
        await retry.Service.ReplayAsync("image_object_deletion", image.Id, "operator", "retry", 1);
        Assert.Single(await observer.OutboxReplayAudits.ToListAsync());
    }

    [RequiresDockerFact]
    public async Task ConcurrentReplays_LockTheSameRow_AndOnlyOneAuditCommits() {
        await using FoodDiaryDbContext seed = await databaseFixture.CreateDbContextAsync();
        ImageObjectDeletionOutboxMessage image = await SeedImageAsync(seed);
        string connection = seed.Database.GetConnectionString()!;
        var gate = new SaveGateInterceptor();
        var capture = new LockCommandCapture();
        await using FoodDiaryDbContext first = CreateContext(connection, gate);
        await using FoodDiaryDbContext second = CreateContext(connection, capture);
        using var firstScope = new OutboxReplayTestScope(first, Clock);
        using var secondScope = new OutboxReplayTestScope(second, Clock);
        Task<OutboxReplayAuditModel> firstReplay = firstScope.Service.ReplayAsync("image_object_deletion", image.Id, "first", "retry", 1);
        Task<OutboxReplayAuditModel>? secondReplay = null;
        try {
            await gate.Reached.Task.WaitAsync(TimeSpan.FromSeconds(30), TimeProvider.System);
            secondReplay = secondScope.Service.ReplayAsync("image_object_deletion", image.Id, "second", "retry", 1);
            await capture.Reached.Task.WaitAsync(TimeSpan.FromSeconds(30), TimeProvider.System);
            Assert.False(secondReplay.IsCompleted);
        } finally {
            gate.Release.TrySetResult();
        }
        await firstReplay.WaitAsync(TimeSpan.FromSeconds(30), TimeProvider.System);
        Assert.NotNull(secondReplay);
        InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(() => secondReplay.WaitAsync(TimeSpan.FromSeconds(30), TimeProvider.System));
        Assert.Contains("Only a dead-lettered", error.Message, StringComparison.Ordinal);
        await using FoodDiaryDbContext observer = databaseFixture.CreateDbContext(connection);
        Assert.Equal("first", (await observer.OutboxReplayAudits.SingleAsync()).RequestedBy);
        Assert.Null((await observer.ImageObjectDeletionOutbox.SingleAsync()).DeadLetteredOnUtc);
    }

    [RequiresDockerFact]
    public async Task CancellationWhileWaitingForRowLock_DoesNotResetMessageOrWriteAudit() {
        await using FoodDiaryDbContext seed = await databaseFixture.CreateDbContextAsync();
        ImageObjectDeletionOutboxMessage image = await SeedImageAsync(seed);
        await using IDbContextTransaction transaction = await seed.Database.BeginTransactionAsync();
        using var owner = new OutboxReplayTestScope(seed, Clock);
        await owner.Streams.Single(stream => string.Equals(stream.Name, "image_object_deletion", StringComparison.Ordinal)).FindAsync(image.Id, forUpdate: true);
        var capture = new LockCommandCapture();
        await using FoodDiaryDbContext waiting = CreateContext(seed.Database.GetConnectionString()!, capture);
        using var waitingScope = new OutboxReplayTestScope(waiting, Clock);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        Task<OutboxReplayAuditModel> replay = waitingScope.Service.ReplayAsync("image_object_deletion", image.Id, "operator", "retry", 1, cancellation.Token);
        await capture.Reached.Task.WaitAsync(TimeSpan.FromSeconds(30), TimeProvider.System);
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => replay.WaitAsync(TimeSpan.FromSeconds(30), TimeProvider.System));
        Assert.Null(waiting.Database.CurrentTransaction);
        await transaction.RollbackAsync();
        seed.ChangeTracker.Clear();
        Assert.NotNull((await seed.ImageObjectDeletionOutbox.SingleAsync()).DeadLetteredOnUtc);
        Assert.Empty(await seed.OutboxReplayAudits.ToListAsync());
    }

    private static FoodDiaryDbContext CreateContext(string connection, IInterceptor interceptor) =>
        new(new DbContextOptionsBuilder<FoodDiaryDbContext>().UseNpgsql(connection).AddInterceptors(interceptor).Options);

    private static async Task<IOutboxMessage[]> CreateLookupBatchAsync(FoodDiaryDbContext context, string marker, bool deadLettered) {
        var user = User.Create($"{marker}@example.com", "hash");
        var notification = Notification.Create(user.Id, "info", "{}");
        IOutboxMessage[] messages = [
            EmailOutboxMessage.Create(new EmailMessage("from@example.com", "Sender", ["to@example.com"], marker, "body", TextBody: null), Now),
            ImageObjectDeletionOutboxMessage.Create($"users/test/{marker}.webp", Now),
            NotificationWebPushOutboxMessage.Create(notification.Id, Now),
            AchievementEvaluationOutboxMessage.Create(user.Id, Now),
        ];
        foreach (IOutboxMessage message in messages) {
            if (deadLettered) {
                message.MarkDeadLettered($"failure-{marker}", Now);
            }
            context.Add(message);
        }
        context.AddRange(user, notification);
        await context.SaveChangesAsync();
        return messages;
    }

    private static async Task<ImageObjectDeletionOutboxMessage> SeedImageAsync(FoodDiaryDbContext context) {
        var image = ImageObjectDeletionOutboxMessage.Create("users/test/replay.webp", Now.AddMinutes(-2));
        image.MarkDeadLettered("failure", Now.AddMinutes(-1));
        context.Add(image);
        await context.SaveChangesAsync();
        return image;
    }

    [ExcludeFromCodeCoverage]
    private sealed class ReplayTimeProvider : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(Now);
    }

    [ExcludeFromCodeCoverage]
    private sealed class LockCommandCapture : DbCommandInterceptor {
        public List<(string Sql, bool HasTransaction)> Commands { get; } = [];
        public TaskCompletionSource Reached { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default) {
            if (command.CommandText.Contains("FOR UPDATE", StringComparison.Ordinal)) {
                Commands.Add((command.CommandText, command.Transaction is not null));
                Reached.TrySetResult();
            }
            return ValueTask.FromResult(result);
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class FailingSaveInterceptor : SaveChangesInterceptor {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Injected replay save failure.");
    }

    [ExcludeFromCodeCoverage]
    private sealed class SaveGateInterceptor : SaveChangesInterceptor {
        public TaskCompletionSource Reached { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default) {
            Reached.TrySetResult();
            await Release.Task.WaitAsync(TimeSpan.FromSeconds(30), TimeProvider.System, cancellationToken);
            return result;
        }
    }
}
