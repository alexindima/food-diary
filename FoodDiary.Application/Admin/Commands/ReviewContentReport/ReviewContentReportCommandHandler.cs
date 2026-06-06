using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.ContentReports.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Social;

namespace FoodDiary.Application.Admin.Commands.ReviewContentReport;

public sealed class ReviewContentReportCommandHandler(IContentReportRepository reportRepository)
    : ICommandHandler<ReviewContentReportCommand, Result> {
    public async Task<Result> Handle(ReviewContentReportCommand command, CancellationToken cancellationToken) {
        var reportId = (ContentReportId)command.ReportId;
        ContentReport? report = await reportRepository.GetByIdAsync(reportId, asTracking: true, cancellationToken).ConfigureAwait(false);

        if (report is null) {
            return Result.Failure(Errors.ContentReport.NotFound(command.ReportId));
        }

        report.MarkReviewed(command.AdminNote);
        await reportRepository.UpdateAsync(report, cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
