using FoodDiary.ReadModel.Composition;
using FoodDiary.ReadModel.Composition.ContentReports;
using FoodDiary.Application.Abstractions.Common.Abstractions.Events;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Abstractions.ContentReports.Common;
using FoodDiary.Domain.Entities.Social;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.Primitives;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.ContentReports.Infrastructure;
using FoodDiary.Modules.ContentReports.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class SharedReportsContextCompositionIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [Fact]
    public async Task UnsupportedTargetReturnsFalseWithoutDatabaseAccessAsync() {
        await using var central = new FoodDiaryDbContext(new DbContextOptionsBuilder<FoodDiaryDbContext>()
            .UseNpgsql("Host=localhost;Database=model_only").Options);
        var reader = new ContentReportReadService(central);
        Assert.False(await reader.IsReportableAsync(UserId.New(), (ReportTargetType)int.MaxValue, Guid.NewGuid()));
    }

    [RequiresDockerFact]
    public async Task SharedSaveAndComposedReadsPreserveUserCascadeAsync() {
        await using FoodDiaryDbContext central = await databaseFixture.CreateDbContextAsync();
        await using ServiceProvider provider = CreateProvider(central);
        ContentReportsDbContext owned = provider.GetRequiredService<ContentReportsDbContext>();
        IContentReportWriteRepository repository = provider.GetRequiredService<IContentReportWriteRepository>();
        IUnitOfWork unitOfWork = provider.GetRequiredService<IUnitOfWork>();
        var user = User.Create($"reports-{Guid.NewGuid():N}@example.com", "hash");
        central.Users.Add(user);
        await repository.AddAsync(ContentReport.Create(user.Id, ReportTargetType.Recipe, Guid.NewGuid(), "Spam"));
        Assert.Same(central.Database.GetDbConnection(), owned.Database.GetDbConnection());
        Assert.Empty(central.ChangeTracker.Entries<ContentReport>());
        await unitOfWork.SaveChangesAsync();
        Assert.Equal(1, await provider.GetRequiredService<IContentReportReadModelRepository>().CountByStatusAsync(ReportStatus.Pending));
        central.Users.Remove(user);
        await unitOfWork.SaveChangesAsync();
        await using FoodDiaryDbContext read = databaseFixture.CreateDbContext(central.Database.GetConnectionString()!);
        Assert.False(await read.ContentReports.AnyAsync(item => item.UserId == user.Id));
    }

    [RequiresDockerFact]
    public async Task InvalidReporterRollsBackCentralUserAsync() {
        await using FoodDiaryDbContext central = await databaseFixture.CreateDbContextAsync();
        await using ServiceProvider provider = CreateProvider(central);
        var user = User.Create($"reports-rollback-{Guid.NewGuid():N}@example.com", "hash");
        central.Users.Add(user);
        await provider.GetRequiredService<IContentReportWriteRepository>().AddAsync(
            ContentReport.Create(UserId.New(), ReportTargetType.Recipe, Guid.NewGuid(), "Spam"));
        await Assert.ThrowsAsync<DbUpdateException>(() => provider.GetRequiredService<IUnitOfWork>().SaveChangesAsync());
        await using FoodDiaryDbContext read = databaseFixture.CreateDbContext(central.Database.GetConnectionString()!);
        Assert.False(await read.Users.AnyAsync(item => item.Id == user.Id));
        Assert.False(await read.ContentReports.AnyAsync());
    }

    private static ServiceProvider CreateProvider(FoodDiaryDbContext context) {
        var services = new ServiceCollection();
        services.AddInfrastructure(new ConfigurationBuilder().Build());
        services.AddSingleton(context);
        services.AddSingleton<IDomainEventPublisher, NoEvents>();
        services.AddContentReportsModule();
        services.AddReadModelComposition();
        return services.BuildServiceProvider();
    }

    [ExcludeFromCodeCoverage]
    private sealed class NoEvents : IDomainEventPublisher {
        public Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
