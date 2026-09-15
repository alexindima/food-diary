using FoodDiary.Modules.ContentReports.Application.Commands.ReviewContentReport;
using FoodDiary.Modules.ContentReports.Application.Commands.DismissContentReport;
using FoodDiary.Modules.ContentReports.Contracts.Commands.ReviewContentReport;
using FoodDiary.Modules.ContentReports.Contracts.Commands.DismissContentReport;
using FoodDiary.Modules.ContentReports.Application.Abstractions.Common;
using FoodDiary.Modules.ContentReports.Domain.Entities;
using FoodDiary.Modules.ContentReports.Domain.Contracts.Enums;
using FoodDiary.Modules.ContentReports.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.ContentReports.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class ContentReportModerationTests {
    [Fact]
    public async Task ReviewContentReportHandler_WhenReportMissing_ReturnsNotFound() {
        var handler = new ReviewContentReportCommandHandler(
            new CountingContentReportRepository());
        var reportId = Guid.NewGuid();

        Result result = await handler.Handle(new ReviewContentReportCommand(new ContentReportId(reportId), UserId.New(), "handled"), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("ContentReport.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task ReviewContentReportHandler_WithExistingReport_MarksReviewed() {
        var report = ContentReport.Create(
            UserId.New(),
            ReportTargetType.Recipe,
            Guid.NewGuid(),
            "Incorrect content");
        var repository = new CountingContentReportRepository(report);
        var handler = new ReviewContentReportCommandHandler(repository);

        var reviewerUserId = UserId.New();
        Result result = await handler.Handle(new ReviewContentReportCommand(report.Id, reviewerUserId, "  verified  "), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(ReportStatus.Reviewed, report.Status);
        Assert.Equal("verified", report.AdminNote);
        Assert.Equal(reviewerUserId, report.ReviewedByUserId);
        Assert.Equal(1, repository.UpdateCallCount);
    }

    [Fact]
    public async Task DismissContentReportHandler_WhenReportMissing_ReturnsNotFound() {
        var handler = new DismissContentReportCommandHandler(
            new CountingContentReportRepository());
        var reportId = Guid.NewGuid();

        Result result = await handler.Handle(new DismissContentReportCommand(new ContentReportId(reportId), UserId.New(), "duplicate"), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("ContentReport.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task DismissContentReportHandler_WithExistingReport_MarksDismissed() {
        var report = ContentReport.Create(
            UserId.New(),
            ReportTargetType.Recipe,
            Guid.NewGuid(),
            "Incorrect content");
        var repository = new CountingContentReportRepository(report);
        var handler = new DismissContentReportCommandHandler(repository);

        var reviewerUserId = UserId.New();
        Result result = await handler.Handle(new DismissContentReportCommand(report.Id, reviewerUserId, "  duplicate  "), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(ReportStatus.Dismissed, report.Status);
        Assert.Equal("duplicate", report.AdminNote);
        Assert.Equal(reviewerUserId, report.ReviewedByUserId);
        Assert.Equal(1, repository.UpdateCallCount);
    }

    [Fact]
    public async Task DismissContentReportHandler_WhenReportAlreadyResolved_ReturnsConflictWithoutUpdate() {
        var report = ContentReport.Create(UserId.New(), ReportTargetType.Recipe, Guid.NewGuid(), "Incorrect content");
        report.MarkReviewed(UserId.New(), "verified");
        var repository = new CountingContentReportRepository(report);
        var handler = new DismissContentReportCommandHandler(repository);

        Result result = await handler.Handle(
            new DismissContentReportCommand(report.Id, UserId.New(), "overwrite"),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Multiple(
            () => Assert.Equal("ContentReport.AlreadyResolved", result.Error.Code),
            () => Assert.Equal(ReportStatus.Reviewed, report.Status),
            () => Assert.Equal(0, repository.UpdateCallCount));
    }

    [ExcludeFromCodeCoverage]
    private sealed class CountingContentReportRepository(ContentReport? storedReport = null) : IContentReportWriteRepository {
        public int UpdateCallCount { get; private set; }

        public Task<ContentReport?> GetByIdAsync(ContentReportId id, bool asTracking = false, CancellationToken cancellationToken = default) =>
            Task.FromResult(storedReport?.Id == id ? storedReport : null);

        public Task UpdateAsync(ContentReport report, CancellationToken cancellationToken = default) {
            Assert.Same(storedReport, report);
            UpdateCallCount++;
            return Task.CompletedTask;
        }

        public Task<ContentReport> AddAsync(ContentReport report, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<bool> HasUserReportedAsync(UserId userId, ReportTargetType targetType, Guid targetId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
