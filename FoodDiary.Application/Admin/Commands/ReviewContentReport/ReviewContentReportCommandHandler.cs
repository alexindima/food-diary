using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.ContentReports.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Admin.Commands.ReviewContentReport;

public sealed class ReviewContentReportCommandHandler(IContentReportRepository reportRepository)
    : ICommandHandler<ReviewContentReportCommand, Result> {
    public async Task<Result> Handle(ReviewContentReportCommand command, CancellationToken cancellationToken) {
        var reportId = (ContentReportId)command.ReportId;
        var report = await reportRepository.GetByIdAsync(reportId, asTracking: true, cancellationToken);

        if (report is null) {
            return Result.Failure(Errors.ContentReport.NotFound(command.ReportId));
        }

        report.MarkReviewed(command.AdminNote);
        await reportRepository.UpdateAsync(report, cancellationToken);

        return Result.Success();
    }
}
