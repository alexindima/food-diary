using FoodDiary.Modules.Marketing.Infrastructure;
using FoodDiary.Modules.Marketing.Domain.Entities.Tracking;
using FoodDiary.Outbox.Infrastructure;
using FoodDiary.Persistence.Runtime;
using FoodDiary.Audit.Infrastructure;
using FoodDiary.Email.Infrastructure;
using FoodDiary.Persistence.Runtime.Persistence;
using FoodDiary.Application.Abstractions.Common.Abstractions.Events;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Modules.Marketing.Application.Abstractions.Common;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Domain.Primitives;
using FoodDiary.Infrastructure.Persistence;

using FoodDiary.Modules.Marketing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class SharedMarketingContextCompositionIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [Fact]
    public void ModelContainsOnlyOwnedAttributionTable() {
        using var context = new MarketingDbContext(new DbContextOptionsBuilder<MarketingDbContext>()
            .UseNpgsql("Host=localhost;Database=model_only").Options);
        Assert.Equal(typeof(MarketingAttributionEvent), Assert.Single(context.Model.GetEntityTypes()).ClrType);
        Assert.Equal("MarketingAttributionEvents", context.Model.FindEntityType(typeof(MarketingAttributionEvent))!.GetTableName());
    }

    [RequiresDockerTheory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SharedSavePreservesUniquenessAndRollsBackCentralChangesAsync(bool repeatEventId) {
        await using FoodDiaryDbContext central = await databaseFixture.CreateDbContextAsync();
        await using ServiceProvider provider = CreateProvider(central);
        MarketingDbContext marketing = provider.GetRequiredService<MarketingDbContext>();
        IMarketingAttributionEventRepository repository = provider.GetRequiredService<IMarketingAttributionEventRepository>();
        IUnitOfWork unitOfWork = provider.GetRequiredService<IUnitOfWork>();
        var user = User.Create($"marketing-{Guid.NewGuid():N}@example.com", "hash");
        MarketingAttributionEventRecord record = CreateRecord(DateTime.UtcNow) with {
            EventType = "signup_completed", UserId = user.Id.Value,
        };
        central.Users.Add(user);
        await repository.AddAsync(record);
        Assert.Same(central.Database.GetDbConnection(), marketing.Database.GetDbConnection());
        Assert.Empty(central.ChangeTracker.Entries<MarketingAttributionEvent>());
        await unitOfWork.SaveChangesAsync();
        marketing.ChangeTracker.Clear();
        await using FoodDiaryDbContext read = databaseFixture.CreateDbContext(central.Database.GetConnectionString()!);
        Assert.True(await read.Users.AnyAsync(item => item.Id == user.Id));
        Assert.Equal(1, await read.MarketingAttributionEvents.CountAsync(item => item.UserId == user.Id.Value));
        var rolledBackUser = User.Create($"marketing-rollback-{Guid.NewGuid():N}@example.com", "hash");
        central.Users.Add(rolledBackUser);
        await repository.AddAsync(record with { EventId = repeatEventId ? record.EventId : Guid.NewGuid() });
        await Assert.ThrowsAsync<DbUpdateException>(() => unitOfWork.SaveChangesAsync());
        Assert.False(await read.Users.AnyAsync(item => item.Id == rolledBackUser.Id));
        Assert.Equal(1, await read.MarketingAttributionEvents.CountAsync(item => item.UserId == user.Id.Value));
    }

    [RequiresDockerFact]
    public async Task OwnedContextPreservesBoundedRetentionAndRangeReadsAsync() {
        await using FoodDiaryDbContext central = await databaseFixture.CreateDbContextAsync();
        await using ServiceProvider provider = CreateProvider(central);
        IMarketingAttributionEventRepository repository = provider.GetRequiredService<IMarketingAttributionEventRepository>();
        DateTime cutoff = new(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        await repository.AddAsync(CreateRecord(cutoff.AddDays(-2)));
        await repository.AddAsync(CreateRecord(cutoff.AddDays(-1)));
        await repository.AddAsync(CreateRecord(cutoff));
        await provider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        Assert.Equal(1, await repository.DeleteOlderThanAsync(cutoff, 1));
        Assert.Equal(1, await repository.DeleteOlderThanAsync(cutoff, 1));
        Assert.Equal(0, await repository.DeleteOlderThanAsync(cutoff, 1));
        IMarketingAttributionRangeReadRepository rangeReader = provider.GetRequiredService<IMarketingAttributionRangeReadRepository>();
        var filter = new MarketingAttributionRangeFilter(cutoff, cutoff.AddDays(1), 1, 50, EventType: null, Channel: null, Search: null);
        MarketingAttributionRangeRecord result = await rangeReader.GetRangeAsync(filter, CancellationToken.None);
        Assert.Equal(1, result.EventTotal);
        Assert.Equal(cutoff, Assert.Single(result.Current.RecentEvents).OccurredAtUtc);
    }

    private static MarketingAttributionEventRecord CreateRecord(DateTime occurredAtUtc) => new(
        "page_landing", occurredAtUtc, UserId: null, "anonymous", "session", "/",
        ReferrerHost: null, UtmSource: null, UtmMedium: null, UtmCampaign: null,
        UtmContent: null, UtmTerm: null, BuildVersion: null, EventId: Guid.NewGuid());

    private static ServiceProvider CreateProvider(FoodDiaryDbContext context) {
        var services = new ServiceCollection();
        services.AddInfrastructure(new ConfigurationBuilder().Build()).AddOutboxProcessing(new ConfigurationBuilder().Build()).AddAuditInfrastructure().AddEmailInfrastructure().AddOutboxReplayManagement();
        services.AddSingleton(context);
        services.AddSingleton<SharedPersistenceDbContext>(context);
        services.AddSingleton<IDomainEventPublisher, NoEvents>();
        services.AddMarketingModule();
        return services.BuildServiceProvider();
    }

    [ExcludeFromCodeCoverage]
    private sealed class NoEvents : IDomainEventPublisher {
        public Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
