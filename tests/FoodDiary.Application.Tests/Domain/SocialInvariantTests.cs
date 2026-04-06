using FoodDiary.Domain.Entities.Social;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Domain;

public class SocialInvariantTests {
    [Fact]
    public void RecipeLike_Create_WithEmptyUserId_Throws() {
        Assert.Throws<ArgumentException>(() =>
            RecipeLike.Create(UserId.Empty, RecipeId.New()));
    }

    [Fact]
    public void RecipeLike_Create_WithEmptyRecipeId_Throws() {
        Assert.Throws<ArgumentException>(() =>
            RecipeLike.Create(UserId.New(), RecipeId.Empty));
    }

    [Fact]
    public void RecipeLike_Create_WithValidIds_Succeeds() {
        var userId = UserId.New();
        var recipeId = RecipeId.New();

        var like = RecipeLike.Create(userId, recipeId);

        Assert.Equal(userId, like.UserId);
        Assert.Equal(recipeId, like.RecipeId);
    }

    [Fact]
    public void ContentReport_Create_WithEmptyUserId_Throws() {
        Assert.Throws<ArgumentException>(() =>
            ContentReport.Create(UserId.Empty, ReportTargetType.Recipe, Guid.NewGuid(), "spam"));
    }

    [Fact]
    public void ContentReport_Create_WithEmptyTargetId_Throws() {
        Assert.Throws<ArgumentException>(() =>
            ContentReport.Create(UserId.New(), ReportTargetType.Recipe, Guid.Empty, "spam"));
    }

    [Fact]
    public void ContentReport_Create_WithBlankReason_Throws() {
        Assert.Throws<ArgumentException>(() =>
            ContentReport.Create(UserId.New(), ReportTargetType.Recipe, Guid.NewGuid(), "   "));
    }

    [Fact]
    public void ContentReport_Create_WithTooLongReason_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ContentReport.Create(UserId.New(), ReportTargetType.Recipe, Guid.NewGuid(), new string('r', 1001)));
    }

    [Fact]
    public void ContentReport_Create_TrimsReasonAndSetsPending() {
        var report = ContentReport.Create(
            UserId.New(), ReportTargetType.Recipe, Guid.NewGuid(), "  spam content  ");

        Assert.Equal("spam content", report.Reason);
        Assert.Equal(ReportStatus.Pending, report.Status);
        Assert.Null(report.AdminNote);
        Assert.Null(report.ReviewedAtUtc);
    }

    [Fact]
    public void ContentReport_MarkReviewed_SetsStatusAndNote() {
        var report = ContentReport.Create(
            UserId.New(), ReportTargetType.Recipe, Guid.NewGuid(), "spam");

        report.MarkReviewed("  Confirmed spam  ");

        Assert.Equal(ReportStatus.Reviewed, report.Status);
        Assert.Equal("Confirmed spam", report.AdminNote);
        Assert.NotNull(report.ReviewedAtUtc);
    }

    [Fact]
    public void ContentReport_MarkDismissed_SetsStatusAndNote() {
        var report = ContentReport.Create(
            UserId.New(), ReportTargetType.Recipe, Guid.NewGuid(), "spam");

        report.MarkDismissed(null);

        Assert.Equal(ReportStatus.Dismissed, report.Status);
        Assert.Null(report.AdminNote);
        Assert.NotNull(report.ReviewedAtUtc);
    }
}
