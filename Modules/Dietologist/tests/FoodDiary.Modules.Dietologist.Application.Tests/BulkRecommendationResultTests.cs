using FoodDiary.Modules.Dietologist.Application.Models;

namespace FoodDiary.Modules.Dietologist.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class BulkRecommendationResultTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SuccessfulRecipientsPreserveTheClientAndRecommendationIdentity(bool alreadyProcessed) {
        var client = Guid.NewGuid();
        var recommendation = Guid.NewGuid();
        BulkRecommendationRecipientResultModel result = alreadyProcessed
            ? new BulkRecommendationRecipientResultModel.AlreadyProcessed(client, recommendation)
            : new BulkRecommendationRecipientResultModel.Created(client, recommendation);

        Assert.Equal(client, result.ClientUserId);
        Assert.Equal(recommendation, result.RecommendationId);
        Assert.True(result.Succeeded);
        Assert.Equal(alreadyProcessed, result.WasAlreadyProcessed);
        Assert.Null(result.ErrorCode);
    }

    [Fact]
    public void RejectedAndMissingRecipientOutcomesRetainTheirFailureSemantics() {
        var clientId = Guid.NewGuid();
        BulkRecommendationRecipientResultModel rejected = new BulkRecommendationRecipientResultModel.Rejected(clientId, "User.NotFound");
        Assert.Equal(clientId, rejected.ClientUserId);
        Assert.False(rejected.Succeeded);
        Assert.Null(rejected.RecommendationId);
        Assert.Equal("User.NotFound", rejected.ErrorCode);
        BulkRecommendationRecipientResultModel absent = default;
        Assert.Throws<InvalidOperationException>(() => absent.ClientUserId);
    }
}
