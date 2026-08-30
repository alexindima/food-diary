using FoodDiary.Domain.Entities.Social;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.ContentReports.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class ContentReportContractTests {
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
}
