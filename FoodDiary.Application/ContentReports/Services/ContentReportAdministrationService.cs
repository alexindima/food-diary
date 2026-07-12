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
        string? adminNote,
        CancellationToken cancellationToken) =>
        UpdateStatusAsync(reportId, adminNote, static (report, note) => report.MarkReviewed(note), cancellationToken);

    public Task<Result> MarkDismissedAsync(
        ContentReportId reportId,
        string? adminNote,
        CancellationToken cancellationToken) =>
        UpdateStatusAsync(reportId, adminNote, static (report, note) => report.MarkDismissed(note), cancellationToken);

    private async Task<Result> UpdateStatusAsync(
        ContentReportId reportId,
        string? adminNote,
        Action<ContentReport, string?> transition,
        CancellationToken cancellationToken) {
        ContentReport? report = await reportRepository
            .GetByIdAsync(reportId, asTracking: true, cancellationToken)
            .ConfigureAwait(false);
        if (report is null) {
            return Result.Failure(Errors.ContentReport.NotFound(reportId.Value));
        }

        transition(report, adminNote);
        await reportRepository.UpdateAsync(report, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
