using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.ContentReports.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Social;

namespace FoodDiary.Application.Admin.Commands.DismissContentReport;

public sealed class DismissContentReportCommandHandler(IContentReportWriteRepository reportRepository)
    : ICommandHandler<DismissContentReportCommand, Result> {
    public async Task<Result> Handle(DismissContentReportCommand command, CancellationToken cancellationToken) {
        var reportId = (ContentReportId)command.ReportId;
        ContentReport? report = await reportRepository.GetByIdAsync(reportId, asTracking: true, cancellationToken).ConfigureAwait(false);

        if (report is null) {
            return Result.Failure(Errors.ContentReport.NotFound(command.ReportId));
        }

        report.MarkDismissed(command.AdminNote);
        await reportRepository.UpdateAsync(report, cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
