using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Tests.Domain;

[ExcludeFromCodeCoverage]
public sealed class RecommendationBulkDispatchInvariantTests {
    [Fact]
    public void Create_WithValidValues_NormalizesAndStoresDispatch() {
        var dietologistUserId = UserId.New();
        var clientUserId = UserId.New();
        var recommendationId = RecommendationId.New();

        var dispatch = RecommendationBulkDispatch.Create(
            dietologistUserId,
            clientUserId,
            recommendationId,
            "  dispatch-key  ");

        Assert.Multiple(
            () => Assert.NotEqual(default(RecommendationBulkDispatchId), dispatch.Id),
            () => Assert.Equal(dietologistUserId, dispatch.DietologistUserId),
            () => Assert.Equal(clientUserId, dispatch.ClientUserId),
            () => Assert.Equal(recommendationId, dispatch.RecommendationId),
            () => Assert.Equal("dispatch-key", dispatch.IdempotencyKey),
            () => Assert.NotEqual(default, dispatch.CreatedOnUtc));
    }

    [Fact]
    public void Create_RejectsEmptyEntityIds() {
        Assert.Multiple(
            () => Assert.Throws<ArgumentException>(() => RecommendationBulkDispatch.Create(
                UserId.Empty, UserId.New(), RecommendationId.New(), "key")),
            () => Assert.Throws<ArgumentException>(() => RecommendationBulkDispatch.Create(
                UserId.New(), UserId.Empty, RecommendationId.New(), "key")),
            () => Assert.Throws<ArgumentException>(() => RecommendationBulkDispatch.Create(
                UserId.New(), UserId.New(), RecommendationId.Empty, "key")));
    }

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
