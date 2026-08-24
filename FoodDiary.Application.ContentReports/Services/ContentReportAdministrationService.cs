using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.ContentReports.Common;
using FoodDiary.Application.ContentReports.Common;
using FoodDiary.Domain.Entities.Social;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.ContentReports.Services;

public sealed class ContentReportAdministrationService(IContentReportWriteRepository reportRepository)
    : IContentReportAdministrationService {
    public Task<Result> MarkReviewedAsync(
        ContentReportId reportId,
        UserId reviewerUserId,
        string? adminNote,
        CancellationToken cancellationToken) =>
        UpdateStatusAsync(reportId, reviewerUserId, adminNote, static (report, reviewer, note) => report.MarkReviewed(reviewer, note), cancellationToken);

    public Task<Result> MarkDismissedAsync(
        ContentReportId reportId,
        UserId reviewerUserId,
        string? adminNote,
        CancellationToken cancellationToken) =>
        UpdateStatusAsync(reportId, reviewerUserId, adminNote, static (report, reviewer, note) => report.MarkDismissed(reviewer, note), cancellationToken);

    private async Task<Result> UpdateStatusAsync(
        ContentReportId reportId,
        UserId reviewerUserId,
        string? adminNote,
        Action<ContentReport, UserId, string?> transition,
        CancellationToken cancellationToken) {
        ContentReport? report = await reportRepository
            .GetByIdAsync(reportId, asTracking: true, cancellationToken)
            .ConfigureAwait(false);
        if (report is null) {
            return Result.Failure(Errors.ContentReport.NotFound(reportId.Value));
        }

        if (report.Status != Domain.Enums.ReportStatus.Pending) {
            return Result.Failure(Errors.ContentReport.AlreadyResolved);
        }

        transition(report, reviewerUserId, adminNote);
        await reportRepository.UpdateAsync(report, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
