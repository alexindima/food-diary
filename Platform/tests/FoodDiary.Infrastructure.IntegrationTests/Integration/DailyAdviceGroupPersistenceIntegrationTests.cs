using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.DailyAdvices.Application.Abstractions.Models;
using FoodDiary.Modules.DailyAdvices.Domain.Entities.Content;
using FoodDiary.Modules.DailyAdvices.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class DailyAdviceGroupPersistenceIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task UpgradePreservesLegacyRowsAndAssignsDistinctGroups() {
        string connectionString = await databaseFixture.CreateIsolatedDatabaseAsync();
        await using FoodDiaryDbContext context = databaseFixture.CreateDbContext(connectionString);
        IMigrator migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync("20260917010000_ConvertOtherMealsToSnack");
        await context.Database.ExecuteSqlRawAsync("""
            DELETE FROM "DailyAdvices";
            INSERT INTO "DailyAdvices" ("Id", "Value", "Locale", "Weight", "CreatedOnUtc") VALUES
            ('11111111-1111-1111-1111-111111111111', 'First', 'ru', 1, NOW()),
            ('22222222-2222-2222-2222-222222222222', 'Second', 'ru', 2, NOW()),
            ('33333333-3333-3333-3333-333333333333', 'Third', 'en', 3, NOW());
            """);
        await migrator.MigrateAsync();
        List<DailyAdvice> rows = await context.DailyAdvices.OrderBy(item => item.Value).ToListAsync();
        Assert.Multiple(() => Assert.Equal(3, rows.Count),
            () => Assert.Equal(3, rows.Select(item => item.GroupId).Distinct().Count()),
            () => Assert.All(rows, item => Assert.NotEqual(Guid.Empty, item.GroupId)),
            () => Assert.Equal(new[] { "First", "Second", "Third" }, rows.Select(item => item.Value), StringComparer.Ordinal));
        Assert.False(context.Database.HasPendingModelChanges());
    }

    [RequiresDockerFact]
    public async Task GroupWriteAndDeletePersistBothLanguagesWithoutTouchingOtherGroups() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        await context.DailyAdvices.ExecuteDeleteAsync();
        var ru = DailyAdvice.Create("Russian", "ru");
        var en = DailyAdvice.Create("English", "en");
        en.AssignGroup(ru.GroupId);
        var other = DailyAdvice.Create("Other", "en");
        var writer = new DailyAdviceWriteRepository(context.DailyAdvices);
        await writer.AddRangeAsync([ru, en, other]);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var reader = new DailyAdviceRepository(context.DailyAdvices);
        IReadOnlyList<DailyAdviceReadModel> projections = await reader.GetAllReadModelsAsync();
        Assert.Equal(2, projections.Count(item => item.GroupId == ru.GroupId));
        Assert.Empty(context.ChangeTracker.Entries());
        IReadOnlyList<DailyAdvice> group = await writer.GetGroupAsync(ru.GroupId);
        Assert.Equal(2, group.Count);
        writer.RemoveRange(group);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        DailyAdvice remaining = Assert.Single(await context.DailyAdvices.ToListAsync());
        Assert.Equal(other.Id, remaining.Id);
    }
}
