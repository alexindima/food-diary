using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Images.PersistenceModel.Images;
using FoodDiary.Outbox.Infrastructure.Options;
using FoodDiary.Outbox.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class ScopedOutboxClaimIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerTheory]
    [InlineData("combined")]
    [InlineData("absent")]
    [InlineData("empty")]
    public async Task InvalidOrEmptyDynamicScopesCannotDispatchOrClaimPendingMessages(string scope) {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var pending = ImageObjectDeletionOutboxMessage.Create("scope-boundary", DateTime.UtcNow);
        context.Add(pending);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        int dispatches = 0;
        int? processed = null;

        Exception? failure = await Record.ExceptionAsync(async () => processed = await OutboxProcessingEngine.ProcessDueAsync(
            context, context.ImageObjectDeletionOutbox, "\"ImageObjectDeletionOutbox\"", "scope_boundary", 10,
            new OutboxProcessingOptions(), TimeProvider.System, (_, _) => { dispatches++; return Task.CompletedTask; },
            message => message.Id, NullLogger.Instance, allowedMessageIds: scope is "combined" ? [pending.Id] : null,
            selectMessageIdsAsync: (_, _) => Task.FromResult<IReadOnlyCollection<Guid>>(scope is "absent" ? null! : [])));

        if (scope is "combined") {
            Assert.IsType<ArgumentException>(failure);
        } else if (scope is "absent") {
            Assert.IsType<InvalidOperationException>(failure);
        } else {
            Assert.Null(failure);
            Assert.Equal(0, processed);
        }
        Assert.Equal(0, dispatches);
        ImageObjectDeletionOutboxMessage unchanged = await context.ImageObjectDeletionOutbox.AsNoTracking().SingleAsync();
        Assert.Null(unchanged.LockedBy);
        Assert.Null(unchanged.ProcessedOnUtc);
        Assert.Equal(0, unchanged.AttemptCount);
    }

    [RequiresDockerFact]
    public async Task ExplicitMessageScope_DoesNotClaimAnEarlierUnrelatedMessage() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var unrelated = ImageObjectDeletionOutboxMessage.Create("unrelated-image", DateTime.UtcNow.AddMinutes(-1));
        var owned = ImageObjectDeletionOutboxMessage.Create("owned-image", DateTime.UtcNow);
        context.ImageObjectDeletionOutbox.AddRange(unrelated, owned);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        List<Guid> dispatched = [];

        int processed = await OutboxProcessingEngine.ProcessDueAsync(context, context.ImageObjectDeletionOutbox,
            "\"ImageObjectDeletionOutbox\"", "scoped_test", 10, new OutboxProcessingOptions(), TimeProvider.System,
            (message, _) => { dispatched.Add(message.Id); return Task.CompletedTask; }, message => message.Id, NullLogger.Instance,
            allowedMessageIds: [owned.Id]);

        ImageObjectDeletionOutboxMessage untouched = await context.ImageObjectDeletionOutbox.AsNoTracking().SingleAsync(message => message.Id == unrelated.Id);
        ImageObjectDeletionOutboxMessage completed = await context.ImageObjectDeletionOutbox.AsNoTracking().SingleAsync(message => message.Id == owned.Id);
        Assert.Multiple(
            () => Assert.Equal(1, processed),
            () => Assert.Equal(owned.Id, Assert.Single(dispatched)),
            () => Assert.Null(untouched.LockedBy),
            () => Assert.Null(untouched.ProcessedOnUtc),
            () => Assert.Equal(0, untouched.AttemptCount),
            () => Assert.NotNull(completed.ProcessedOnUtc));
    }
}
