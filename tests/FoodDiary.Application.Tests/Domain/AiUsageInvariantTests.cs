using FoodDiary.Domain.Entities.Ai;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Tests.Domain;

public class AiUsageInvariantTests
{
    [Fact]
    public void Create_WithEmptyUserId_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            AiUsage.Create(
                UserId.Empty,
                operation: "meal_scan",
                model: "gpt-4.1-mini",
                inputTokens: 10,
                outputTokens: 5,
                totalTokens: 15));
    }

    [Fact]
    public void Create_WithBlankOperation_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            AiUsage.Create(
                UserId.New(),
                operation: "   ",
                model: "gpt-4.1-mini",
                inputTokens: 10,
                outputTokens: 5,
                totalTokens: 15));
    }

    [Fact]
    public void Create_WithBlankModel_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            AiUsage.Create(
                UserId.New(),
                operation: "meal_scan",
                model: "  ",
                inputTokens: 10,
                outputTokens: 5,
                totalTokens: 15));
    }

    [Fact]
    public void Create_WithTooLongOperation_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            AiUsage.Create(
                UserId.New(),
                operation: new string('o', 33),
                model: "gpt-4.1-mini",
                inputTokens: 10,
                outputTokens: 5,
                totalTokens: 15));
    }

    [Fact]
    public void Create_WithTooLongModel_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            AiUsage.Create(
                UserId.New(),
                operation: "meal_scan",
                model: new string('m', 65),
                inputTokens: 10,
                outputTokens: 5,
                totalTokens: 15));
    }

    [Theory]
    [InlineData(-1, 0, 0)]
    [InlineData(0, -1, 0)]
    [InlineData(0, 0, -1)]
    public void Create_WithNegativeTokens_Throws(
        int inputTokens,
        int outputTokens,
        int totalTokens)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            AiUsage.Create(
                UserId.New(),
                operation: "meal_scan",
                model: "gpt-4.1-mini",
                inputTokens: inputTokens,
                outputTokens: outputTokens,
                totalTokens: totalTokens));
    }

    [Fact]
    public void Create_WithTotalTokensLowerThanInputAndOutputSum_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            AiUsage.Create(
                UserId.New(),
                operation: "meal_scan",
                model: "gpt-4.1-mini",
                inputTokens: 10,
                outputTokens: 5,
                totalTokens: 14));
    }

    [Fact]
    public void Create_WithValidValues_TrimAndStoreFields()
    {
        var usage = AiUsage.Create(
            UserId.New(),
            operation: "  meal_scan  ",
            model: "  gpt-4.1-mini  ",
            inputTokens: 10,
            outputTokens: 5,
            totalTokens: 20);

        Assert.Equal("meal_scan", usage.Operation);
        Assert.Equal("gpt-4.1-mini", usage.Model);
        Assert.Equal(10, usage.InputTokens);
        Assert.Equal(5, usage.OutputTokens);
        Assert.Equal(20, usage.TotalTokens);
        Assert.NotEqual(Guid.Empty, usage.Id);
        Assert.NotEqual(default, usage.CreatedOnUtc);
    }
}
