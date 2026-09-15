using FoodDiary.Modules.ContentReports.Application.Common;
using FoodDiary.Modules.ContentReports.Application.Abstractions.Common;
using FoodDiary.Modules.ContentReports.Contracts.Commands.ReviewContentReport;
using FoodDiary.Modules.ContentReports.Domain.Contracts.Enums;
using FoodDiary.Modules.ContentReports.Domain.Entities;
using FoodDiary.Mediator;
using FoodDiary.Results;

namespace FoodDiary.Modules.ContentReports.Application.Commands.ReviewContentReport;

public sealed class ReviewContentReportCommandHandler(IContentReportWriteRepository reportRepository) : IRequestHandler<ReviewContentReportCommand, Result> {
    public async Task<Result> Handle(ReviewContentReportCommand request, CancellationToken cancellationToken) {
        ContentReport? report = await reportRepository
            .GetByIdAsync(request.ReportId, asTracking: true, cancellationToken)
            .ConfigureAwait(false);
        if (report is null) {
            return Result.Failure(ContentReportErrors.NotFound(request.ReportId.Value));
        }

        if (report.Status != ReportStatus.Pending) {
            return Result.Failure(ContentReportErrors.AlreadyResolved);
        }

        report.MarkReviewed(request.ReviewerUserId, request.AdminNote);
        await reportRepository.UpdateAsync(report, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
