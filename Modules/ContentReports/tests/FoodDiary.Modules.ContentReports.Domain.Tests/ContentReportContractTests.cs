using FoodDiary.Modules.ContentReports.Domain.Entities;
using FoodDiary.Modules.ContentReports.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.ContentReports.Domain.Contracts.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.ContentReports.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class ContentReportContractTests {
    [Fact]
    public void ReportStatus_PreservesNamesAndNumericValues() {
        Assert.Equal(["Pending", "Reviewed", "Dismissed"], Enum.GetNames<ReportStatus>());
        Assert.Equal([0, 1, 2], Enum.GetValues<ReportStatus>().Select(value => (int)value));
    }

    [Fact]
    public void ReportTargetType_PreservesNamesAndNumericValues() {
        Assert.Equal(["Recipe", "Comment"], Enum.GetNames<ReportTargetType>());
        Assert.Equal([0, 1], Enum.GetValues<ReportTargetType>().Select(value => (int)value));
    }

    [Fact]
    public void ContentReportId_PreservesGuidAcrossConversionFormattingAndEmptySentinel() {
        var value = Guid.Parse("b451a20c-20c5-4d5c-9a2d-827bf7576c25");
        ContentReportId id = new(value);
        Guid converted = id;

        Assert.Multiple(
            () => Assert.Equal(value, converted),
            () => Assert.Equal(value.ToString(), id.ToString()),
            () => Assert.Equal(Guid.Empty, ContentReportId.Empty.Value));
    }

    [Fact]
    public void Create_WithUndefinedTargetType_RejectsInvalidDomainState() {
        Assert.Throws<ArgumentOutOfRangeException>(() => ContentReport.Create(
            userId: UserId.New(), targetType: (ReportTargetType)int.MaxValue,
            targetId: Guid.NewGuid(), reason: "Reason"));
    }

    [Fact]
    public void Resolve_RequiresReviewerAndPendingStatus() {
        var report = ContentReport.Create(
            userId: UserId.New(), targetType: ReportTargetType.Recipe,
            targetId: Guid.NewGuid(), reason: "Reason");

        Assert.Throws<ArgumentException>(() => report.MarkReviewed(UserId.Empty, adminNote: null));

        report.MarkDismissed(UserId.New(), adminNote: "Not actionable");

        Assert.Multiple(
            () => Assert.Equal(ReportStatus.Dismissed, report.Status),
            () => Assert.Throws<InvalidOperationException>(() => report.MarkReviewed(UserId.New(), adminNote: null)));
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

        Assert.Multiple(
            () => Assert.Equal("spam content", report.Reason),
            () => Assert.Equal(ReportStatus.Pending, report.Status),
            () => Assert.Null(report.AdminNote),
            () => Assert.Null(report.ReviewedAtUtc));
    }

    [Fact]
    public void ContentReport_MarkReviewed_SetsStatusAndNote() {
        var report = ContentReport.Create(
            UserId.New(), ReportTargetType.Recipe, Guid.NewGuid(), "spam");

        report.MarkReviewed(UserId.New(), "  Confirmed spam  ");

        Assert.Equal(ReportStatus.Reviewed, report.Status);
        Assert.Equal("Confirmed spam", report.AdminNote);
        Assert.NotNull(report.ReviewedAtUtc);
    }

    [Fact]
    public void ContentReport_MarkDismissed_SetsStatusAndNote() {
        var report = ContentReport.Create(
            UserId.New(), ReportTargetType.Recipe, Guid.NewGuid(), "spam");

        report.MarkDismissed(UserId.New(), adminNote: null);

        Assert.Equal(ReportStatus.Dismissed, report.Status);
        Assert.Null(report.AdminNote);
        Assert.NotNull(report.ReviewedAtUtc);
    }

    [Fact]
    public void ContentReport_ResolveTwice_ThrowsAndPreservesFirstDecision() {
        var report = ContentReport.Create(UserId.New(), ReportTargetType.Recipe, Guid.NewGuid(), "Spam");
        var firstReviewer = UserId.New();
        report.MarkReviewed(firstReviewer, "confirmed");

        Assert.Throws<InvalidOperationException>(() => report.MarkDismissed(UserId.New(), "changed"));

        Assert.Multiple(
            () => Assert.Equal(ReportStatus.Reviewed, report.Status),
            () => Assert.Equal(firstReviewer, report.ReviewedByUserId),
            () => Assert.Equal("confirmed", report.AdminNote));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Resolve_WithTooLongNote_PreservesPendingState(bool dismiss) {
        var report = ContentReport.Create(UserId.New(), ReportTargetType.Recipe, Guid.NewGuid(), "Reason");
        var reviewer = UserId.New();
        string note = new('x', 2001);
        Assert.Throws<ArgumentOutOfRangeException>(() => {
            if (dismiss) {
                report.MarkDismissed(reviewer, note);
            } else {
                report.MarkReviewed(reviewer, note);
            }
        });
        Assert.Multiple(
            () => Assert.Equal(ReportStatus.Pending, report.Status),
            () => Assert.Null(report.AdminNote),
            () => Assert.Null(report.ReviewedByUserId),
            () => Assert.Null(report.ReviewedAtUtc));
    }
}
