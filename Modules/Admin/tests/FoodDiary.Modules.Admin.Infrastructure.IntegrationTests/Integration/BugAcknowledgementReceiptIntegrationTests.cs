using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Infrastructure.Integrations;
using FoodDiary.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class BugAcknowledgementReceiptIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task Record_PersistsReceiptAndToleratesDuplicateFromAnotherContext() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        await using ServiceProvider provider = Provider(context);
        IBugAcknowledgementReceipts receipts = provider.GetRequiredService<IBugAcknowledgementReceipts>();
        var id = Guid.NewGuid();
        Assert.False(await receipts.ContainsAsync(id, CancellationToken.None));
        await receipts.RecordAsync(id, CancellationToken.None);
        Assert.True(await receipts.ContainsAsync(id, CancellationToken.None));
        await using FoodDiaryDbContext other = databaseFixture.CreateDbContext(context.Database.GetConnectionString()!);
        await using ServiceProvider otherProvider = Provider(other);
        await otherProvider.GetRequiredService<IBugAcknowledgementReceipts>().RecordAsync(id, CancellationToken.None);
        Assert.Empty(other.ChangeTracker.Entries<BugAcknowledgementReceipt>());
        Assert.Equal(1, await other.BugAcknowledgementReceipts.CountAsync(x => x.InboxId == id));
    }

    [RequiresDockerFact]
    public async Task Record_UnrelatedWriteFailureIsRethrownAndReceiptDetached() {
        await using FoodDiaryDbContext schema = await databaseFixture.CreateDbContextAsync();
        var failure = new FailingSaveInterceptor();
        DbContextOptions<FoodDiaryDbContext> options = new DbContextOptionsBuilder<FoodDiaryDbContext>()
            .UseNpgsql(schema.Database.GetConnectionString()).AddInterceptors(failure).Options;
        await using var context = new FoodDiaryDbContext(options);
        await using ServiceProvider provider = Provider(context);
        IBugAcknowledgementReceipts receipts = provider.GetRequiredService<IBugAcknowledgementReceipts>();
        var id = Guid.NewGuid();
        DbUpdateException error = await Assert.ThrowsAsync<DbUpdateException>(() => receipts.RecordAsync(id, CancellationToken.None));
        Assert.Same(failure.Error, error);
        Assert.NotNull(error.StackTrace);
        Assert.Contains(nameof(FailingSaveInterceptor.SavingChangesAsync), error.StackTrace, StringComparison.Ordinal);
        Assert.Empty(context.ChangeTracker.Entries<BugAcknowledgementReceipt>());
        Assert.False(await receipts.ContainsAsync(id, CancellationToken.None));
    }

    private static ServiceProvider Provider(FoodDiaryDbContext context) {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
            ["MailInboxClient:BaseUrl"] = "https://inbox.example.com",
        }).Build();
        var services = new ServiceCollection();
        services.AddSingleton(context);
        services.AddAdminMailInboxIntegration(configuration);
        return services.BuildServiceProvider();
    }

    [ExcludeFromCodeCoverage]
    private sealed class FailingSaveInterceptor : SaveChangesInterceptor {
        public DbUpdateException Error { get; } = new("Simulated persistence failure");
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default) => throw Error;
    }
}
