using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Tests.Domain;

[ExcludeFromCodeCoverage]
public sealed class RecommendationCommentInvariantTests {
    [Fact]
    public void Create_WithValidInput_TrimsAndStoresImmutableMessage() {
        var recommendationId = RecommendationId.New();
        var authorUserId = UserId.New();

        var comment = RecommendationComment.Create(
            recommendationId,
            authorUserId,
            "  Please clarify the protein target.  ");

        Assert.Equal(recommendationId, comment.RecommendationId);
        Assert.Equal(authorUserId, comment.AuthorUserId);
        Assert.Equal("Please clarify the protein target.", comment.Text);
        Assert.NotEqual(RecommendationCommentId.Empty, comment.Id);
    }

    [Fact]
    public void Create_WithMissingIdentityOrText_Throws() {
        Assert.Throws<ArgumentException>(() =>
            RecommendationComment.Create(RecommendationId.Empty, UserId.New(), "Message"));
        Assert.Throws<ArgumentException>(() =>
            RecommendationComment.Create(RecommendationId.New(), UserId.Empty, "Message"));
        Assert.Throws<ArgumentException>(() =>
            RecommendationComment.Create(RecommendationId.New(), UserId.New(), "  "));
    }

    [Fact]
    public void Create_WithTextOverLimit_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            RecommendationComment.Create(
                RecommendationId.New(),
                UserId.New(),
                new string('a', 2001)));
    }
}
