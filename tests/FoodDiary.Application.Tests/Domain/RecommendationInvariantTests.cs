using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Domain;

public class RecommendationInvariantTests {
    [Fact]
    public void Create_WithEmptyDietologistUserId_Throws() {
        Assert.Throws<ArgumentException>(() =>
            Recommendation.Create(UserId.Empty, UserId.New(), "Eat more veggies"));
    }

    [Fact]
    public void Create_WithEmptyClientUserId_Throws() {
        Assert.Throws<ArgumentException>(() =>
            Recommendation.Create(UserId.New(), UserId.Empty, "Eat more veggies"));
    }

    [Fact]
    public void Create_WithBlankText_Throws() {
        Assert.Throws<ArgumentException>(() =>
            Recommendation.Create(UserId.New(), UserId.New(), "   "));
    }

    [Fact]
    public void Create_WithTooLongText_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Recommendation.Create(UserId.New(), UserId.New(), new string('a', 2001)));
    }

    [Fact]
    public void Create_TrimsText() {
        var rec = Recommendation.Create(UserId.New(), UserId.New(), "  Eat more veggies  ");

        Assert.Equal("Eat more veggies", rec.Text);
    }

    [Fact]
    public void Create_SetsIsReadToFalse() {
        var rec = Recommendation.Create(UserId.New(), UserId.New(), "Eat more veggies");

        Assert.False(rec.IsRead);
        Assert.Null(rec.ReadAtUtc);
    }

    [Fact]
    public void MarkAsRead_SetsIsReadAndTimestamp() {
        var rec = Recommendation.Create(UserId.New(), UserId.New(), "Eat more veggies");

        rec.MarkAsRead();

        Assert.True(rec.IsRead);
        Assert.NotNull(rec.ReadAtUtc);
    }

    [Fact]
    public void MarkAsRead_WhenAlreadyRead_IsIdempotent() {
        var rec = Recommendation.Create(UserId.New(), UserId.New(), "Eat more veggies");
        rec.MarkAsRead();
        var firstReadAt = rec.ReadAtUtc;

        rec.MarkAsRead();

        Assert.Equal(firstReadAt, rec.ReadAtUtc);
    }
}
