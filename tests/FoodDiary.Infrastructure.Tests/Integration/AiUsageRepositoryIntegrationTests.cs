using System.Reflection;
using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Application.Abstractions.Ai.Common;
using FoodDiary.Domain.Entities.Ai;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Ai;

namespace FoodDiary.Infrastructure.Tests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class AiUsageRepositoryIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task GetSummaryAsync_AggregatesTotalsAndBreakdownsAgainstPostgres() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create("ai-summary@example.com", "hash");
        context.Users.Add(user);

        var inRangeFirst = AiUsage.Create(user.Id, "vision", "gpt-4.1-mini", 10, 20, 30);
        var inRangeSecond = AiUsage.Create(user.Id, "nutrition", "gpt-4.1", 5, 7, 12);
        var outOfRange = AiUsage.Create(user.Id, "vision", "gpt-4.1-mini", 100, 200, 300);

        SetCreatedOnUtc(inRangeFirst, new DateTime(2026, 3, 28, 10, 0, 0, DateTimeKind.Utc));
        SetCreatedOnUtc(inRangeSecond, new DateTime(2026, 3, 28, 12, 0, 0, DateTimeKind.Utc));
        SetCreatedOnUtc(outOfRange, new DateTime(2026, 3, 27, 23, 59, 0, DateTimeKind.Utc));

        context.AiUsages.AddRange(inRangeFirst, inRangeSecond, outOfRange);
        await context.SaveChangesAsync();

        var repository = new AiUsageRepository(context);

        AiUsageSummary summary = await repository.GetSummaryAsync(
            new DateTime(2026, 3, 28, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 3, 29, 0, 0, 0, DateTimeKind.Utc),
            CancellationToken.None);

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
    public async Task AddAsyncAndTotals_WhenNoRows_ReturnZeroes() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create($"ai-empty-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var repository = new AiUsageRepository(context);
        await repository.AddAsync(AiUsage.Create(user.Id, "vision", "gpt-test", 1, 2, 3));

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
        var repository = new AiPromptTemplateRepository(context);
        AiPromptTemplate nutrition = await repository.AddAsync(AiPromptTemplate.Create("Nutrition", "EN", "Estimate nutrients"));
        await repository.AddAsync(AiPromptTemplate.Create("Vision", "ru", "Analyze image", isActive: false));

        IReadOnlyList<AiPromptTemplate> all = await repository.GetAllAsync();
        AiPromptTemplate? byKey = await repository.GetByKeyAsync("nutrition", "en");
        AiPromptTemplate? tracked = await repository.GetByIdAsync(nutrition.Id, asTracking: true);
        Assert.NotNull(tracked);
        tracked.Update("Estimate nutrients precisely", isActive: false);
        await repository.UpdateAsync(tracked);
        AiPromptTemplate? updated = await repository.GetByIdAsync(nutrition.Id);

        Assert.Equal(["nutrition", "vision"], [.. all.Select(template => template.Key)]);
        Assert.Equal(nutrition.Id, byKey?.Id);
        Assert.Equal(2, updated?.Version);
        Assert.False(updated?.IsActive);
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
