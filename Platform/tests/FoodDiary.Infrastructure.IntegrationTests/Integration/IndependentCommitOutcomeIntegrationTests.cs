using System.Data.Common;
using FoodDiary.Email.Contracts.Email.Common;
using FoodDiary.Email.PersistenceModel;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Modules.Ai.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Ai.Infrastructure.Persistence;
using FoodDiary.Modules.Ai.PersistenceModel;
using FoodDiary.Modules.Images.Domain.Entities.Assets;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Outbox.Infrastructure.Options;
using FoodDiary.Outbox.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class IndependentCommitOutcomeIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task QuotaReservation_WithSuccessfulCommitAndLostAcknowledgement_ReturnsAcquiredOnce() {
        await using FoodDiaryDbContext database = await databaseFixture.CreateDbContextAsync();
        var user = User.Create("quota-commit@example.test", "hash");
        database.Users.Add(user);
        await database.SaveChangesAsync();
        DateTime now = DateTime.UtcNow;
        var period = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var fault = new LostCommitAcknowledgement();
        var repository = new AiQuotaRepository(CreateOptions<AiDbContext>(database, fault), TimeProvider.System);
        var request = new AiQuotaReservationRequest(new string('A', 64), user.Id, period, "nutrition",
            100, 50, 1000, 1000, now.AddMinutes(10));

        AiQuotaReservationStatus result = await repository.ReserveAsync(request);

        Assert.Equal(AiQuotaReservationStatus.Acquired, result);
        Assert.True(fault.WasLost);
        await using var check = new AiDbContext(CreateOptions<AiDbContext>(database));
        AiQuotaReservation reservation = Assert.Single(await check.AiQuotaReservations.AsNoTracking().ToListAsync());
        AiQuotaPeriod ledger = Assert.Single(await check.AiQuotaPeriods.AsNoTracking().ToListAsync());
        Assert.Multiple(
            () => Assert.Equal(request.RequestId, reservation.RequestId),
            () => Assert.Equal(100, ledger.ReservedInputTokens),
            () => Assert.Equal(50, ledger.ReservedOutputTokens),
            () => Assert.Equal(0, ledger.ConsumedInputTokens),
            () => Assert.Equal(0, ledger.ConsumedOutputTokens));
        Assert.Equal(AiQuotaReservationStatus.InProgress, await repository.ReserveAsync(request));
    }

    [RequiresDockerTheory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task RecognitionClaim_WithSuccessfulCommitAndLostAcknowledgement_ReturnsTheOriginalJob(int count) {
        await using FoodDiaryDbContext database = await databaseFixture.CreateDbContextAsync();
        var user = User.Create("recognition-commit@example.test", "hash");
        var image = ImageAsset.Create(user.Id, "recognition/commit.jpg", "https://example.test/commit.jpg");
        image.Confirm();
        database.Users.Add(user);
        database.ImageAssets.Add(image);
        await database.SaveChangesAsync();
        var seed = new FoodRecognitionJobStore(CreateOptions<AiDbContext>(database), TimeProvider.System);
        DateTime now = DateTime.UtcNow;
        FoodRecognitionJobModel[] jobs = [.. Enumerable.Range(0, count).Select(index =>
            new FoodRecognitionJobModel(Guid.NewGuid(), user.Id.Value, image.Id.Value, image.Url,
                Description: null, "Queued", now.AddSeconds(index - count), now))];
        foreach (FoodRecognitionJobModel job in jobs) {
            Assert.True((await seed.CreateAsync(job, CancellationToken.None)).IsSuccess);
        }
        var fault = new LostCommitAcknowledgement();
        var store = new FoodRecognitionJobStore(CreateOptions<AiDbContext>(database, fault), TimeProvider.System);

        FoodRecognitionJobModel claimed = Assert.IsType<FoodRecognitionJobModel>(await store.ClaimAsync(CancellationToken.None));

        Assert.Equal(jobs[0].Id, claimed.Id);
        Assert.Equal("Running", claimed.Status);
        Assert.True(fault.WasLost);
        foreach (FoodRecognitionJobModel job in jobs.Skip(1)) {
            FoodRecognitionJobModel queued = Assert.IsType<FoodRecognitionJobModel>(await seed.GetAsync(
                user.Id, new FoodRecognitionJobId(job.Id), CancellationToken.None));
            Assert.Equal("Queued", queued.Status);
        }
    }

    [RequiresDockerTheory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task OutboxClaim_WithSuccessfulCommitAndLostAcknowledgement_DispatchesEveryDueMessage(int count) {
        await using FoodDiaryDbContext database = await databaseFixture.CreateDbContextAsync();
        DateTime now = DateTime.UtcNow;
        EmailOutboxMessage[] pending = [.. Enumerable.Range(0, count).Select(index => EmailOutboxMessage.Create(
            new EmailMessage("sender@example.test", "Fixture", ["recipient@example.test"], "Commit fixture", "<p>Fixture</p>", TextBody: null),
            now.AddSeconds(index - count)))];
        database.EmailOutbox.AddRange(pending);
        await database.SaveChangesAsync();
        var fault = new LostCommitAcknowledgement();
        await using var processor = new FoodDiaryDbContext(CreateOptions<FoodDiaryDbContext>(database, fault));
        var dispatched = new List<Guid>();

        int processed = await OutboxProcessingEngine.ProcessDueAsync(processor, processor.EmailOutbox,
            "\"EmailOutbox\"", "email", count, new OutboxProcessingOptions(), TimeProvider.System,
            (message, _) => { dispatched.Add(message.Id); return Task.CompletedTask; },
            message => message.Id, NullLogger.Instance);

        Assert.True(fault.WasLost);
        Assert.Equal(count, processed);
        Assert.Equal(pending.Select(message => message.Id).Order(), dispatched.Order());
        database.ChangeTracker.Clear();
        EmailOutboxMessage[] persisted = await database.EmailOutbox.AsNoTracking().ToArrayAsync();
        Assert.Equal(count, persisted.Length);
        Assert.All(persisted, message => Assert.Multiple(
            () => Assert.NotNull(message.ProcessedOnUtc),
            () => Assert.Null(message.LockedBy),
            () => Assert.Null(message.LockedUntilUtc),
            () => Assert.Null(message.DeadLetteredOnUtc)));
    }

    private static DbContextOptions<TContext> CreateOptions<TContext>(FoodDiaryDbContext database,
        DbTransactionInterceptor? interceptor = null) where TContext : DbContext {
        DbContextOptionsBuilder<TContext> options = new DbContextOptionsBuilder<TContext>()
            .UseNpgsql(database.Database.GetConnectionString(), provider =>
                provider.EnableRetryOnFailure(2, TimeSpan.Zero, errorCodesToAdd: null));
        if (interceptor is not null) {
            options.AddInterceptors(interceptor);
        }
        return options.Options;
    }

    [ExcludeFromCodeCoverage]
    private sealed class LostCommitAcknowledgement : DbTransactionInterceptor {
        public bool WasLost { get; private set; }
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData,
            CancellationToken cancellationToken = default) {
            if (!WasLost) {
                WasLost = true;
                throw new TimeoutException("The PostgreSQL COMMIT succeeded before its acknowledgement was lost.");
            }
            return Task.CompletedTask;
        }
    }
}
