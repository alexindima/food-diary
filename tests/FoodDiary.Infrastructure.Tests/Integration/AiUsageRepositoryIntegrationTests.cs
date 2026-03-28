using FoodDiary.Domain.Entities.Ai;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Infrastructure.Persistence.Ai;

namespace FoodDiary.Infrastructure.Tests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
public sealed class AiUsageRepositoryIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task GetSummaryAsync_AggregatesTotalsAndBreakdownsAgainstPostgres() {
        await using var context = await databaseFixture.CreateDbContextAsync();
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

        var summary = await repository.GetSummaryAsync(
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

    private static void SetCreatedOnUtc(AiUsage usage, DateTime createdOnUtc) {
        var property = typeof(AiUsage)
            .GetProperty("CreatedOnUtc", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("CreatedOnUtc property was not found.");

        property.SetValue(usage, createdOnUtc);
    }
}
