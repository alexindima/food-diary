using FoodDiary.Application.Abstractions.ContentReports.Common;
using FoodDiary.Domain.Entities.Social;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;
using FoodDiary.Mediator;

namespace FoodDiary.Application.ContentReports.Commands.ReviewContentReport;

public sealed class ReviewContentReportCommandHandler(IContentReportWriteRepository reportRepository) : IRequestHandler<ReviewContentReportCommand, Result> {
    public Task<Result> Handle(ReviewContentReportCommand request, CancellationToken cancellationToken) {
        ContentReportId reportId = request.ReportId;
        UserId reviewerUserId = request.ReviewerUserId;
        string? adminNote = request.AdminNote;
        return UpdateStatusAsync(reportId, reviewerUserId, adminNote, static (report, reviewer, note) => report.MarkReviewed(reviewer, note), cancellationToken);
    }

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
            return Result.Failure(ContentReportErrors.NotFound(reportId.Value));
        }

        if (report.Status != Domain.Enums.ReportStatus.Pending) {
            return Result.Failure(ContentReportErrors.AlreadyResolved);
        }

        transition(report, reviewerUserId, adminNote);
        await reportRepository.UpdateAsync(report, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

}
