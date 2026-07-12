using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.ContentReports.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Admin.Commands.DismissContentReport;

public sealed class DismissContentReportCommandHandler(IContentReportAdministrationService administrationService)
    : ICommandHandler<DismissContentReportCommand, Result> {
    public async Task<Result> Handle(DismissContentReportCommand command, CancellationToken cancellationToken) {
        return await administrationService
            .MarkDismissedAsync((ContentReportId)command.ReportId, command.AdminNote, cancellationToken)
            .ConfigureAwait(false);
    }
}
