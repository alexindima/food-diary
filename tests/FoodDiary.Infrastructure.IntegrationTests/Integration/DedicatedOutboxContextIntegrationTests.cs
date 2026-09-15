using FoodDiary.Outbox.Infrastructure.Options;
using FoodDiary.Outbox.Infrastructure.Persistence;
using FoodDiary.Application.Abstractions.Email.Common;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class DedicatedOutboxContextIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OwnerContextPersistsCompletionOrRetryAsync(bool failDispatch) {
        await using FoodDiaryDbContext central = await databaseFixture.CreateDbContextAsync();
        await using var owned = new DedicatedOutboxContext(new DbContextOptionsBuilder<DedicatedOutboxContext>()
            .UseNpgsql(central.Database.GetDbConnection()).Options);
        Assert.Equal(typeof(EmailOutboxMessage), Assert.Single(owned.Model.GetEntityTypes()).ClrType);
        EmailOutboxMessage message = CreateMessage();
        owned.EmailOutbox.Add(message);
        await owned.SaveChangesAsync();
        bool dispatched = false;
        int processed = await RunAsync(owned, owned.EmailOutbox, (claimed, _) => {
            dispatched = true;
            Assert.NotNull(claimed.LockedBy);
            return failDispatch ? Task.FromException(new InvalidOperationException("Synthetic failure")) : Task.CompletedTask;
        });
        Assert.True(dispatched);
        Assert.Equal(failDispatch ? 0 : 1, processed);
        EmailOutboxMessage saved = await central.EmailOutbox.AsNoTracking().SingleAsync(item => item.Id == message.Id);
        Assert.Equal(failDispatch ? 1 : 0, saved.AttemptCount);
        Assert.Equal(!failDispatch, saved.ProcessedOnUtc.HasValue);
        Assert.Null(saved.LockedBy);
        Assert.Empty(owned.ChangeTracker.Entries());
        Assert.Equal(0, await RunAsync(owned, owned.EmailOutbox, (_, _) => throw new InvalidOperationException("Not due")));
    }

    [RequiresDockerFact]
    public async Task OwnerContextRejectsPendingChangesAndExistingTransactionAsync() {
        await using FoodDiaryDbContext central = await databaseFixture.CreateDbContextAsync();
        await using var owned = new DedicatedOutboxContext(new DbContextOptionsBuilder<DedicatedOutboxContext>()
            .UseNpgsql(central.Database.GetDbConnection()).Options);
        owned.EmailOutbox.Add(CreateMessage());
        await Assert.ThrowsAsync<InvalidOperationException>(() => RunAsync(owned, owned.EmailOutbox, (_, _) => Task.CompletedTask));
        Assert.Single(owned.ChangeTracker.Entries());
        await owned.SaveChangesAsync();
        await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = await owned.Database.BeginTransactionAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => RunAsync(owned, owned.EmailOutbox, (_, _) => Task.CompletedTask));
        await transaction.RollbackAsync();
    }

    [RequiresDockerFact]
    public async Task SharedContextStillRejectsPendingModuleChangesAsync() {
        await using FoodDiaryDbContext central = await databaseFixture.CreateDbContextAsync();
        await using DedicatedOutboxContext owned = central.CreateModuleContext<DedicatedOutboxContext>(static options => new DedicatedOutboxContext(options));
        owned.EmailOutbox.Add(CreateMessage());
        await Assert.ThrowsAsync<InvalidOperationException>(() => RunAsync(central, central.EmailOutbox, (_, _) => Task.CompletedTask));
        Assert.Single(owned.ChangeTracker.Entries());
    }

    private static Task<int> RunAsync(DbContext context, DbSet<EmailOutboxMessage> messages,
        Func<EmailOutboxMessage, CancellationToken, Task> dispatchAsync) =>
        OutboxProcessingEngine.ProcessDueAsync(context, messages, "\"EmailOutbox\"", "email", 1,
            new OutboxProcessingOptions(), TimeProvider.System, dispatchAsync, message => message.Id, NullLogger.Instance);

    private static EmailOutboxMessage CreateMessage() => EmailOutboxMessage.Create(
        new EmailMessage("sender@example.com", "Test", ["recipient@example.com"], "Test", "Body", TextBody: null),
        DateTime.UtcNow.AddMinutes(-1));

    [ExcludeFromCodeCoverage]
    private sealed class DedicatedOutboxContext(DbContextOptions<DedicatedOutboxContext> options) : DbContext(options) {
        public DbSet<EmailOutboxMessage> EmailOutbox => Set<EmailOutboxMessage>();

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            modelBuilder.ApplyEmailPersistenceModel();
        }
    }
}
