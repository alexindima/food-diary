using FoodDiary.Modules.ContentReports.Infrastructure;
using Npgsql;
using FoodDiary.Modules.ContentReports.Domain.Entities;
using FoodDiary.Modules.ContentReports.Domain.Contracts.Enums;
using FoodDiary.ReadModel.Composition;
using FoodDiary.ReadModel.Composition.ContentReports;
using FoodDiary.Application.Abstractions.Common.Abstractions.Events;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Modules.ContentReports.Application.Abstractions.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Primitives;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;
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
        DbUpdateException exception = await Assert.ThrowsAsync<DbUpdateException>(() => provider.GetRequiredService<IUnitOfWork>().SaveChangesAsync());
        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, Assert.IsType<PostgresException>(exception.InnerException).SqlState);
        await using FoodDiaryDbContext read = databaseFixture.CreateDbContext(central.Database.GetConnectionString()!);
        Assert.False(await read.Users.AnyAsync(item => item.Id == user.Id));
        Assert.False(await read.ContentReports.AnyAsync());
    }

    [RequiresDockerTheory]
    [InlineData(ReportTargetType.Recipe)]
    [InlineData(ReportTargetType.Comment)]
    public async Task CompetingReportsReturnConflictAndRollBackLosingScopeAsync(ReportTargetType targetType) {
        await using FoodDiaryDbContext seed = await databaseFixture.CreateDbContextAsync();
        var reporter = User.Create("reporter@example.com", "hash");
        seed.Users.Add(reporter);
        await seed.SaveChangesAsync();
        string connection = seed.Database.GetConnectionString()!;
        await using FoodDiaryDbContext first = databaseFixture.CreateDbContext(connection);
        await using FoodDiaryDbContext second = databaseFixture.CreateDbContext(connection);
        await using ServiceProvider firstProvider = CreateProvider(first);
        await using ServiceProvider secondProvider = CreateProvider(second);
        var targetId = Guid.NewGuid();
        IContentReportWriteRepository firstReports = firstProvider.GetRequiredService<IContentReportWriteRepository>();
        IContentReportWriteRepository secondReports = secondProvider.GetRequiredService<IContentReportWriteRepository>();
        // Both requests pass the duplicate check before either transaction saves.
        Assert.False(await firstReports.HasUserReportedAsync(reporter.Id, targetType, targetId));
        Assert.False(await secondReports.HasUserReportedAsync(reporter.Id, targetType, targetId));
        await firstReports.AddAsync(ContentReport.Create(reporter.Id, targetType, targetId, "Spam"));
        await secondReports.AddAsync(ContentReport.Create(reporter.Id, targetType, targetId, "Spam"));
        var firstSideEffect = User.Create("report-first@example.com", "hash");
        var secondSideEffect = User.Create("report-second@example.com", "hash");
        first.Users.Add(firstSideEffect);
        second.Users.Add(secondSideEffect);

        Exception?[] outcomes = await Task.WhenAll(
            Record.ExceptionAsync(() => firstProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync()),
            Record.ExceptionAsync(() => secondProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync()));

        Assert.Single(outcomes, exception => exception is null);
        DbUpdateConcurrencyException conflict = Assert.IsType<DbUpdateConcurrencyException>(Assert.Single(outcomes.OfType<Exception>()));
        DbUpdateException providerFailure = Assert.IsType<DbUpdateException>(conflict.InnerException);
        PostgresException postgres = Assert.IsType<PostgresException>(providerFailure.InnerException);
        Assert.Equal(PostgresErrorCodes.UniqueViolation, postgres.SqlState);
        Assert.Equal("IX_ContentReports_UserId_TargetType_TargetId", postgres.ConstraintName);
        await using FoodDiaryDbContext read = databaseFixture.CreateDbContext(connection);
        Assert.Equal(1, await read.ContentReports.CountAsync(report => report.UserId == reporter.Id && report.TargetType == targetType && report.TargetId == targetId));
        Assert.Equal(outcomes[0] is null, await read.Users.AnyAsync(user => user.Id == firstSideEffect.Id));
        Assert.Equal(outcomes[1] is null, await read.Users.AnyAsync(user => user.Id == secondSideEffect.Id));
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
