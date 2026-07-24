using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Tests.Domain;

[ExcludeFromCodeCoverage]
public sealed class RecommendationBulkDispatchInvariantTests {
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_RejectsMissingIdempotencyKey(string key) {
        Assert.Throws<ArgumentException>(() => RecommendationBulkDispatch.Create(
            UserId.New(),
            UserId.New(),
            RecommendationId.New(),
            key));
    }

    [Fact]
    public void Create_RejectsLongIdempotencyKey() {
        Assert.Throws<ArgumentOutOfRangeException>(() => RecommendationBulkDispatch.Create(
            UserId.New(),
            UserId.New(),
            RecommendationId.New(),
            new string('x', 101)));
    }
}
