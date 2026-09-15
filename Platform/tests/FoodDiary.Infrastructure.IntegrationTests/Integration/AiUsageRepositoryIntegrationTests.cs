using Microsoft.EntityFrameworkCore;
using FoodDiary.ReadModel.Composition.Ai;
using System.Reflection;
using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Modules.Ai.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Ai.Infrastructure.Persistence;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class AiUsageRepositoryIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task GetSummaryForUserAsync_ExcludesOtherUsersFromEveryBreakdown() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var included = User.Create("ai-filter-included@example.com", "hash");
        var excluded = User.Create("ai-filter-excluded@example.com", "hash");
        context.Users.AddRange(included, excluded);
        var first = AiUsage.Create(included.Id, "nutrition", "included-model", 2, 3, 5);
        var second = AiUsage.Create(excluded.Id, "vision", "excluded-model", 10, 20, 30);
        DateTime from = new(2026, 3, 28, 0, 0, 0, DateTimeKind.Utc);
        SetCreatedOnUtc(first, from);
        SetCreatedOnUtc(second, from);
        context.AiUsages.AddRange(first, second);
        await context.SaveChangesAsync();
        var repository = new AiUsageQuery(context);
        AiUsageSummary result = await repository.GetSummaryForUserAsync(from, from.AddDays(1), included.Id, CancellationToken.None);
        Assert.Equal(5, result.TotalTokens);
        Assert.Equal(5, Assert.Single(result.ByDay).TotalTokens);
        Assert.Single(result.ByModel);
        Assert.Single(result.ByOperation);
        Assert.Equal(included.Id, Assert.Single(result.ByUser).UserId);
    }

    [RequiresDockerFact]
    public async Task GetSummaryAsync_AggregatesTotalsAndBreakdownsAgainstPostgres() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create("ai-summary@example.com", "hash");
        context.Users.Add(user);

        var inRangeFirst = AiUsage.Create(user.Id, "vision", "gpt-4.1-mini", 10, 20, 30);
        var inRangeSecond = AiUsage.Create(user.Id, "nutrition", "gpt-4.1", 5, 7, 12);
        var atExclusiveEnd = AiUsage.Create(user.Id, "vision", "boundary-model", 1000, 2000, 3000);
        var outOfRange = AiUsage.Create(user.Id, "vision", "gpt-4.1-mini", 100, 200, 300);

        SetCreatedOnUtc(inRangeFirst, new DateTime(2026, 3, 28, 10, 0, 0, DateTimeKind.Utc));
        SetCreatedOnUtc(inRangeSecond, new DateTime(2026, 3, 28, 12, 0, 0, DateTimeKind.Utc));
        SetCreatedOnUtc(outOfRange, new DateTime(2026, 3, 27, 23, 59, 0, DateTimeKind.Utc));

        SetCreatedOnUtc(atExclusiveEnd, new DateTime(2026, 3, 29, 0, 0, 0, DateTimeKind.Utc));
        context.AiUsages.AddRange(inRangeFirst, inRangeSecond, outOfRange, atExclusiveEnd);
        await context.SaveChangesAsync();

        var repository = new AiUsageQuery(context);

        context.ChangeTracker.Clear();
        AiUsageSummary summary = await repository.GetSummaryAsync(
            new DateTime(2026, 3, 28, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 3, 29, 0, 0, 0, DateTimeKind.Utc),
            CancellationToken.None);

        Assert.Empty(context.ChangeTracker.Entries());
        Assert.Equal(42, summary.TotalTokens);
        Assert.Equal(15, summary.InputTokens);
        Assert.Equal(27, summary.OutputTokens);
        Assert.Single(summary.ByDay);
        Assert.Equal(42, summary.ByDay[0].TotalTokens);
        Assert.Equal(2, summary.ByOperation.Count);
        Assert.Equal(2, summary.ByModel.Count);
        Assert.Single(summary.ByUser);
        Assert.Equal(user.Id, summary.ByUser[0].UserId);
        Assert.Equal("ai-summary@example.com", summary.ByUser[0].Email);
    }

    [RequiresDockerFact]
    public async Task Totals_WhenNoRows_ReturnZeroes() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"ai-empty-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var repository = new AiUsageQuery(context);

        AiUsageSummary emptySummary = await repository.GetSummaryAsync(
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            CancellationToken.None);
        AiUsageTotals emptyTotals = await repository.GetUserTotalsAsync(
            user.Id,
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            CancellationToken.None);

        Assert.Equal(0, emptySummary.TotalTokens);
        Assert.Empty(emptySummary.ByDay);
        Assert.Equal(0, emptyTotals.InputTokens);
        Assert.Equal(0, emptyTotals.OutputTokens);
    }

    [RequiresDockerFact]
    public async Task AiPromptTemplateRepository_AddsQueriesOrdersAndUpdatesTemplates() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var repository = new AiPromptTemplateRepository(context.AiPromptTemplates);
        AiPromptTemplate nutrition = await repository.AddAsync(AiPromptTemplate.Create("Nutrition", "EN", "Estimate nutrients"));
        await repository.AddAsync(AiPromptTemplate.Create("Vision", "ru", "Analyze image", isActive: false));
        await context.SaveChangesAsync();

        IReadOnlyList<AiPromptTemplateReadModel> allReadModels = await repository.GetAllReadModelsAsync();
        AiPromptTemplate? byKey = await repository.GetByKeyAsync("nutrition", "en");
        AiPromptTemplate? tracked = await repository.GetByKeyAsync("nutrition", "en");
        Assert.NotNull(tracked);
        tracked.Update("Estimate nutrients precisely", isActive: false);
        await repository.UpdateAsync(tracked);
        await context.SaveChangesAsync();
        AiPromptTemplate? updated = await repository.GetByKeyAsync("nutrition", "en");

        Assert.Equal(["nutrition", "vision"], [.. allReadModels.Select(template => template.Key)]);
        Assert.Equal(nutrition.Id, byKey?.Id);
        Assert.Equal(2, updated?.Version);
        Assert.False(updated?.IsActive);
        AiPromptRevisionReadModel revision = Assert.Single(await repository.GetRevisionsAsync("nutrition", "en", CancellationToken.None));
        Assert.Equal("Estimate nutrients", revision.PromptText);
        Assert.Equal(1, revision.Version);
        context.ChangeTracker.Clear();
        AiPromptTemplate? restored = await repository.GetByKeyAsync("nutrition", "en");
        Assert.NotNull(restored);
        restored.Update(revision.PromptText, revision.IsActive);
        await context.SaveChangesAsync();
        Assert.Equal(3, restored.Version);
        Assert.Equal(2, (await repository.GetRevisionsAsync("nutrition", "en", CancellationToken.None)).Count);
    }

    [RequiresDockerTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PromptConcurrentUpdate_RejectsStaleWriteAndPreservesRevision(bool activationOnly) {
        await using FoodDiaryDbContext database = await databaseFixture.CreateDbContextAsync();
        database.AiPromptTemplates.Add(AiPromptTemplate.Create("concurrent", "en", "Original"));
        await database.SaveChangesAsync();
        DbContextOptions<AiDbContext> options = new DbContextOptionsBuilder<AiDbContext>()
            .UseNpgsql(database.Database.GetConnectionString()).Options;
        await using var first = new AiDbContext(options);
        await using var second = new AiDbContext(options);
        var firstRepository = new AiPromptTemplateRepository(first.AiPromptTemplates);
        var secondRepository = new AiPromptTemplateRepository(second.AiPromptTemplates);
        AiPromptTemplate winner = Assert.IsType<AiPromptTemplate>(await firstRepository.GetByKeyAsync("concurrent", "en"));
        AiPromptTemplate stale = Assert.IsType<AiPromptTemplate>(await secondRepository.GetByKeyAsync("concurrent", "en"));
        Assert.Single(first.ChangeTracker.Entries<AiPromptTemplate>());
        winner.Update(activationOnly ? "Original" : "First edit", isActive: false);
        stale.Update("Stale edit", isActive: true);
        await first.SaveChangesAsync();

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());

        database.ChangeTracker.Clear();
        AiPromptTemplate saved = await database.AiPromptTemplates.SingleAsync();
        Assert.Equal(activationOnly ? "Original" : "First edit", saved.PromptText);
        Assert.False(saved.IsActive);
        Assert.Equal(activationOnly ? 1 : 2, saved.Version);
        AiPromptRevisionReadModel revision = Assert.Single(await firstRepository.GetRevisionsAsync("concurrent", "en", CancellationToken.None));
        Assert.Equal("Original", revision.PromptText);
        Assert.True(revision.IsActive);
    }

    [RequiresDockerTheory]
    [InlineData("en", true)]
    [InlineData("ru", false)]
    public async Task PromptConcurrentCreate_ConflictsOnlyForSameKeyAndLocale(string secondLocale, bool conflicts) {
        await using FoodDiaryDbContext database = await databaseFixture.CreateDbContextAsync();
        DbContextOptions<AiDbContext> options = new DbContextOptionsBuilder<AiDbContext>()
            .UseNpgsql(database.Database.GetConnectionString()).Options;
        await using var first = new AiDbContext(options);
        await using var second = new AiDbContext(options);
        var firstRepository = new AiPromptTemplateRepository(first.AiPromptTemplates);
        var secondRepository = new AiPromptTemplateRepository(second.AiPromptTemplates);
        Assert.Null(await firstRepository.GetByKeyAsync("create-race", "en"));
        Assert.Null(await secondRepository.GetByKeyAsync("create-race", secondLocale));
        await firstRepository.AddAsync(AiPromptTemplate.Create("create-race", "en", "First"));
        await secondRepository.AddAsync(AiPromptTemplate.Create("create-race", secondLocale, "Second"));

        Exception?[] results = await Task.WhenAll(
            Record.ExceptionAsync(() => first.SaveChangesAsync(acceptAllChangesOnSuccess: false)),
            Record.ExceptionAsync(() => second.SaveChangesAsync(acceptAllChangesOnSuccess: false)));

        if (conflicts) {
            Assert.Single(results, result => result is null);
            DbUpdateConcurrencyException conflict = Assert.IsType<DbUpdateConcurrencyException>(Assert.Single(results, result => result is not null));
            Assert.IsType<DbUpdateException>(conflict.InnerException);
            AiPromptTemplate saved = await database.AiPromptTemplates.SingleAsync();
            Assert.Equal(results[0] is null ? "First" : "Second", saved.PromptText);
            Assert.Equal(1, saved.Version);
        } else {
            Assert.All(results, Assert.Null);
            Assert.Equal(2, await database.AiPromptTemplates.CountAsync());
        }
        Assert.Empty(await database.AiPromptTemplates.AsNoTracking().SelectMany(template => template.Revisions).ToListAsync());
    }

    private static void SetCreatedOnUtc(AiUsage usage, DateTime createdOnUtc) {
        MethodInfo method = typeof(AiUsage).BaseType?
            .GetMethod(
                "SetCreated",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                binder: null,
                [typeof(DateTime)],
                modifiers: null)
            ?? throw new InvalidOperationException("SetCreated(DateTime) method was not found.");

        method.Invoke(usage, [createdOnUtc]);
    }
}
