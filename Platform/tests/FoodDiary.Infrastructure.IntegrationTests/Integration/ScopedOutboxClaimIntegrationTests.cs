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
