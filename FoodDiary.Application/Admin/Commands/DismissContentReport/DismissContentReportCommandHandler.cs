using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.ContentReports.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Admin.Commands.DismissContentReport;

public sealed class DismissContentReportCommandHandler(IContentReportRepository reportRepository)
    : ICommandHandler<DismissContentReportCommand, Result> {
    public async Task<Result> Handle(DismissContentReportCommand command, CancellationToken cancellationToken) {
        var reportId = (ContentReportId)command.ReportId;
        var report = await reportRepository.GetByIdAsync(reportId, asTracking: true, cancellationToken);

        if (report is null) {
            return Result.Failure(Errors.ContentReport.NotFound(command.ReportId));
        }

        report.MarkDismissed(command.AdminNote);
        await reportRepository.UpdateAsync(report, cancellationToken);

        return Result.Success();
    }
}
