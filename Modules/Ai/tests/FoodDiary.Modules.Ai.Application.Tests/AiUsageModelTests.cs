using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Ai.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class AiUsageModelTests {
    [Fact]
    public void AiUsageBreakdown_StoresTokenBreakdown() {
        var breakdown = new AiUsageBreakdown("vision", 30, 10, 20);

        Assert.Equal("vision", breakdown.Key);
        Assert.Equal(30, breakdown.TotalTokens);
        Assert.Equal(10, breakdown.InputTokens);
        Assert.Equal(20, breakdown.OutputTokens);
    }

    [Fact]
    public void AiUsageDailySummary_StoresDailyTokenSummary() {
        var date = new DateOnly(2026, 6, 3);

        var summary = new AiUsageDailySummary(date, 30, 10, 20);

        Assert.Equal(date, summary.Date);
        Assert.Equal(30, summary.TotalTokens);
        Assert.Equal(10, summary.InputTokens);
        Assert.Equal(20, summary.OutputTokens);
    }

    [Fact]
    public void AiUsageUserSummary_StoresUserTokenSummary() {
        var userId = UserId.New();

        var summary = new AiUsageUserSummary(userId, "user@test.com", 30, 10, 20);

        Assert.Equal(userId, summary.UserId);
        Assert.Equal("user@test.com", summary.Email);
        Assert.Equal(30, summary.TotalTokens);
        Assert.Equal(10, summary.InputTokens);
        Assert.Equal(20, summary.OutputTokens);
    }
}
